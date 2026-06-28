// SPDX-License-Identifier: Apache-2.0

/*
 * Pure-C valgrind leak harness for the H3.NET.Native binding.
 *
 * This program mirrors the exact buffer-sizing / ownership patterns that the
 * managed P/Invoke layer uses against libh3. Running it under valgrind proves
 * that the *native usage pattern itself* (caller-owned output buffers, the
 * heap-owning cellsToLinkedMultiPolygon / destroyLinkedMultiPolygon dance, and
 * GeoPolygon construction) is leak-free. Any leak reported by valgrind here
 * is a leak the binding would also exhibit, so it is treated as a CI failure.
 *
 * Every H3Error is checked; the process exits non-zero on the first failure.
 *
 * Usage: leakcheck [iterations]   (default: 50)
 */

#include <h3api.h>

#include <math.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

/* Public-surface units are degrees; the ABI boundary is radians. */
#define DEG_TO_RAD (M_PI / 180.0)

/* Anchor coordinate (San Francisco) used to seed the loop, in degrees. */
#define SEED_LAT_DEG 37.775938728915946
#define SEED_LNG_DEG (-122.41795063018799)

/* Reports an H3Error with context and returns it (non-zero on failure). */
static H3Error check(const char *op, H3Error err) {
    if (err != E_SUCCESS) {
        fprintf(stderr, "FAIL: %s -> H3Error %u (%s)\n", op, err,
                describeH3Error(err));
    }
    return err;
}

/*
 * Exercises latLngToCell / cellToLatLng / cellToBoundary.
 * Output buffers are caller-owned, fixed-size stack structs (no heap).
 * Writes the resolved cell to *outCell for downstream reuse.
 */
static H3Error exercise_scalar(int res, H3Index *outCell) {
    H3Error err;

    LatLng seed = {SEED_LAT_DEG * DEG_TO_RAD, SEED_LNG_DEG * DEG_TO_RAD};
    H3Index cell = H3_NULL;
    err = check("latLngToCell", latLngToCell(&seed, res, &cell));
    if (err != E_SUCCESS) {
        return err;
    }
    if (cell == H3_NULL || !isValidCell(cell)) {
        fprintf(stderr, "FAIL: latLngToCell produced invalid cell\n");
        return E_CELL_INVALID;
    }

    /* Inspection functions return bare int; validate input first (done above). */
    (void)getResolution(cell);
    (void)isPentagon(cell);
    (void)isResClassIII(cell);
    (void)getBaseCellNumber(cell);

    LatLng center = {0};
    err = check("cellToLatLng", cellToLatLng(cell, &center));
    if (err != E_SUCCESS) {
        return err;
    }

    CellBoundary boundary = {0};
    err = check("cellToBoundary", cellToBoundary(cell, &boundary));
    if (err != E_SUCCESS) {
        return err;
    }
    if (boundary.numVerts <= 0 || boundary.numVerts > MAX_CELL_BNDRY_VERTS) {
        fprintf(stderr, "FAIL: cellToBoundary numVerts out of range\n");
        return E_FAILED;
    }

    *outCell = cell;
    return E_SUCCESS;
}

/*
 * Exercises maxGridDiskSize + gridDisk: size, malloc, fill, free.
 * Mirrors the binding's "size then allocate exactly" pattern. Unused slots
 * are H3_NULL (also for pentagons) and must be tolerated.
 */
static H3Error exercise_grid_disk(H3Index origin, int k) {
    H3Error err;

    int64_t maxSize = 0;
    err = check("maxGridDiskSize", maxGridDiskSize(k, &maxSize));
    if (err != E_SUCCESS) {
        return err;
    }
    if (maxSize <= 0) {
        fprintf(stderr, "FAIL: maxGridDiskSize returned %lld\n",
                (long long)maxSize);
        return E_FAILED;
    }

    H3Index *disk = calloc((size_t)maxSize, sizeof(H3Index));
    if (disk == NULL) {
        fprintf(stderr, "FAIL: calloc gridDisk buffer\n");
        return E_MEMORY_ALLOC;
    }

    err = check("gridDisk", gridDisk(origin, k, disk));
    free(disk);
    return err;
}

/*
 * Exercises maxPolygonToCellsSize + polygonToCells.
 * Builds a GeoPolygon with a single GeoLoop (heap-allocated vertex array,
 * no holes), sizes the output, mallocs it, fills, and frees both buffers.
 * For the non-experimental API, flags MUST be 0.
 */
static H3Error exercise_polygon(int res) {
    H3Error err = E_SUCCESS;
    const uint32_t flags = 0;

    /* A small quadrilateral around the seed point, vertices in radians. */
    const int numVerts = 4;
    LatLng *verts = malloc((size_t)numVerts * sizeof(LatLng));
    if (verts == NULL) {
        fprintf(stderr, "FAIL: malloc GeoLoop verts\n");
        return E_MEMORY_ALLOC;
    }
    const double box[4][2] = {
        {37.813318999983238, -122.409290778685657},
        {37.812995299813685, -122.351619987374090},
        {37.798587370801746, -122.351619987374090},
        {37.797786887053924, -122.409290778685657},
    };
    for (int i = 0; i < numVerts; i++) {
        verts[i].lat = box[i][0] * DEG_TO_RAD;
        verts[i].lng = box[i][1] * DEG_TO_RAD;
    }

    GeoPolygon polygon;
    polygon.geoloop.numVerts = numVerts;
    polygon.geoloop.verts = verts;
    polygon.numHoles = 0;
    polygon.holes = NULL;

    int64_t maxSize = 0;
    err = check("maxPolygonToCellsSize",
                maxPolygonToCellsSize(&polygon, res, flags, &maxSize));
    if (err != E_SUCCESS) {
        free(verts);
        return err;
    }
    if (maxSize <= 0) {
        fprintf(stderr, "FAIL: maxPolygonToCellsSize returned %lld\n",
                (long long)maxSize);
        free(verts);
        return E_FAILED;
    }

    H3Index *cells = calloc((size_t)maxSize, sizeof(H3Index));
    if (cells == NULL) {
        fprintf(stderr, "FAIL: calloc polygonToCells buffer\n");
        free(verts);
        return E_MEMORY_ALLOC;
    }

    err = check("polygonToCells", polygonToCells(&polygon, res, flags, cells));

    free(cells);
    free(verts);
    return err;
}

/*
 * Exercises the heap-owning path: cellsToLinkedMultiPolygon +
 * destroyLinkedMultiPolygon.
 *
 * Ownership contract (verified from src/h3lib/lib/linkedGeo.c):
 *   - The CALLER allocates and owns the head LinkedGeoPolygon (here: stack).
 *   - cellsToLinkedMultiPolygon fills the head and heap-allocates all loops,
 *     coordinate nodes, and any subsequent polygon nodes in the 'next' chain.
 *   - destroyLinkedMultiPolygon frees every child and every non-head polygon
 *     node, then ZEROES the head (*polygon = {0}) rather than freeing it.
 *     It is idempotent: a second call on the zeroed head is a safe no-op.
 *
 * So the head's own storage is freed by the owner (automatic here, since it
 * lives on the stack). We also call destroy twice to prove idempotency.
 */
static H3Error exercise_linked_multipolygon(const H3Index *cells,
                                            int numCells) {
    LinkedGeoPolygon head = {0};

    H3Error err = check("cellsToLinkedMultiPolygon",
                        cellsToLinkedMultiPolygon(cells, numCells, &head));
    if (err != E_SUCCESS) {
        /*
         * On failure the function may have partially built the structure;
         * destroy is safe on a (possibly) partially filled head.
         */
        destroyLinkedMultiPolygon(&head);
        return err;
    }

    /* Free all heap children; zeroes the head. */
    destroyLinkedMultiPolygon(&head);
    /* Idempotent second call must not double-free. */
    destroyLinkedMultiPolygon(&head);

    return E_SUCCESS;
}

int main(int argc, char **argv) {
    long iterations = 50;
    if (argc > 1) {
        char *end = NULL;
        long parsed = strtol(argv[1], &end, 10);
        if (end == argv[1] || parsed <= 0) {
            fprintf(stderr, "usage: %s [iterations>0]\n", argv[0]);
            return 2;
        }
        iterations = parsed;
    }

    const int res = 9;
    const int k = 3;

    for (long iter = 0; iter < iterations; iter++) {
        H3Index cell = H3_NULL;
        if (exercise_scalar(res, &cell) != E_SUCCESS) {
            return 1;
        }
        if (exercise_grid_disk(cell, k) != E_SUCCESS) {
            return 1;
        }
        if (exercise_polygon(res) != E_SUCCESS) {
            return 1;
        }

        /*
         * Build a contiguous gridDisk and feed it to the linked-multipolygon
         * path so the result has real loops/coords to allocate and free.
         */
        int64_t diskMax = 0;
        if (check("maxGridDiskSize", maxGridDiskSize(k, &diskMax)) !=
            E_SUCCESS) {
            return 1;
        }
        H3Index *disk = calloc((size_t)diskMax, sizeof(H3Index));
        if (disk == NULL) {
            fprintf(stderr, "FAIL: calloc disk for multipolygon\n");
            return 1;
        }
        if (check("gridDisk", gridDisk(cell, k, disk)) != E_SUCCESS) {
            free(disk);
            return 1;
        }
        /* Compact the buffer: drop H3_NULL padding slots. */
        int numCells = 0;
        for (int64_t i = 0; i < diskMax; i++) {
            if (disk[i] != H3_NULL) {
                disk[numCells++] = disk[i];
            }
        }
        H3Error linkedErr = exercise_linked_multipolygon(disk, numCells);
        free(disk);
        if (linkedErr != E_SUCCESS) {
            return 1;
        }
    }

    printf("OK: %ld iterations, all H3 calls succeeded, buffers freed\n",
           iterations);
    return 0;
}
