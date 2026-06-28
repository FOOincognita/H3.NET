# Security Policy

## Supported versions

H3.NET.Native is currently a pre-1.0 scaffold and has no released versions yet.
Once releases are published, the **latest published version** is the supported
version for security fixes. Pre-release and previously published versions are
not guaranteed to receive backported fixes prior to 1.0.

| Version | Supported |
| --- | --- |
| Latest published release | Yes |
| Older / pre-release versions | No |

## Reporting a vulnerability

Please report security vulnerabilities privately through **GitHub's private
vulnerability reporting** rather than opening a public issue:

1. Go to https://github.com/FOOincognita/H3.NET.Native
2. Open the **Security** tab.
3. Select **Advisories > Report a vulnerability**.

Provide as much detail as possible, including affected version(s), a description
of the issue, reproduction steps or a proof of concept, and any relevant
environment details (TFM, runtime identifier, OS).

## What to expect

- **Acknowledgement** of your report after it is received.
- **Triage** to confirm and assess the severity and scope.
- Coordination on a fix and, where appropriate, a coordinated disclosure
  timeline and a published advisory once a fix is available.

Please do not publicly disclose the issue until a fix has been released and an
advisory published.

## Native dependency surface

H3.NET.Native bundles a native library, `libh3`, built from Uber H3 (pinned to
tag **v4.5.0**, https://github.com/uber/h3). Because of this, the security
surface includes both the managed binding and the bundled native code.

If a reported issue originates in upstream H3 rather than in this binding, we
will forward or coordinate the report with the upstream project at
[uber/h3](https://github.com/uber/h3) and track the corresponding fix and
version bump here.
