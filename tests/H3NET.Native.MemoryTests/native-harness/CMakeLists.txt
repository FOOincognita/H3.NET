# SPDX-License-Identifier: Apache-2.0
#
# Build definition for the pure-C valgrind leak harness (leakcheck.c).
#
# This harness mirrors the native buffer-sizing / ownership patterns the
# H3NET.Native binding uses against libh3, so valgrind can prove the usage
# pattern is leak-free.
#
# It builds standalone and links against an already-built libh3. By default it
# uses the in-repo build at external/h3/build, matching the layout produced by
# configuring + building the vendored H3 source.
#
# ---------------------------------------------------------------------------
# Configure + build (from this directory):
#
#   cmake -B build -S . \
#     -DH3_BUILD_DIR=../../../external/h3/build
#   cmake --build build
#
# Or point at explicit header/lib locations:
#
#   cmake -B build -S . \
#     -DH3_INCLUDE_DIR=/path/to/h3api.h/dir \
#     -DH3_LIBRARY=/path/to/libh3.so
#
# ---------------------------------------------------------------------------
# Run it (no leak checking; just correctness, exits 0 on success):
#
#   ./build/leakcheck 100
#
# ---------------------------------------------------------------------------
# Run it under valgrind in CI (Linux only -- valgrind is unavailable on macOS):
#
#   valgrind --leak-check=full --show-leak-kinds=all --errors-for-leak-kinds=all \
#            --error-exitcode=1 ./build/leakcheck 100
#
# --error-exitcode=1 makes valgrind exit non-zero on ANY leak/error, failing
# the CI job. The harness itself also exits non-zero on any H3Error.
# ---------------------------------------------------------------------------

cmake_minimum_required(VERSION 3.16)
project(h3net_native_leakcheck C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

# Root of an already-built H3 tree (contains src/h3lib/include and lib/).
set(H3_BUILD_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../../../external/h3/build"
    CACHE PATH "Path to a built external/h3 tree")

# Generated h3api.h lives under <build>/src/h3lib/include after configure.
# The non-generated headers (e.g. constants) live in the source tree.
set(H3_INCLUDE_DIR "${H3_BUILD_DIR}/src/h3lib/include"
    CACHE PATH "Directory containing the generated h3api.h")
set(H3_SRC_INCLUDE_DIR "${H3_BUILD_DIR}/../src/h3lib/include"
    CACHE PATH "Directory containing the non-generated H3 headers")

# Locate libh3 (.so on Linux, .dylib on macOS) inside the build tree unless
# an explicit path was supplied.
find_library(H3_LIBRARY
    NAMES h3 libh3
    HINTS "${H3_BUILD_DIR}/lib"
    NO_DEFAULT_PATH)
if(NOT H3_LIBRARY)
    message(FATAL_ERROR
        "libh3 not found under ${H3_BUILD_DIR}/lib. "
        "Build external/h3 first, or set -DH3_LIBRARY=/path/to/libh3.")
endif()

add_executable(leakcheck leakcheck.c)
target_compile_options(leakcheck PRIVATE -Wall -Wextra -g -O0)
target_include_directories(leakcheck PRIVATE
    "${H3_INCLUDE_DIR}"
    "${H3_SRC_INCLUDE_DIR}")
target_link_libraries(leakcheck PRIVATE "${H3_LIBRARY}" m)

# Embed an rpath to the build lib so the executable finds libh3 at runtime
# without requiring LD_LIBRARY_PATH / DYLD_LIBRARY_PATH.
set_target_properties(leakcheck PROPERTIES
    BUILD_RPATH "${H3_BUILD_DIR}/lib")
