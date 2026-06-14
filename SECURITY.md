# Security Policy

## Supported Versions

During the beta phase only the **latest published version** receives security fixes.

| Version | Supported |
|---------|-----------|
| 0.x (latest) | Yes |
| Older 0.x | No |

## Reporting a Vulnerability

**Please do not open a public GitHub issue for security vulnerabilities.**

Report vulnerabilities privately via
[GitHub Security Advisories](https://github.com/primeBeri/huml-dotnet/security/advisories/new).

You can expect:
- **Acknowledgement within 7 days** of your report.
- A fix or mitigation plan within a reasonable timeframe depending on severity.
- Credit in the changelog if you wish.

## Scope

Huml.Net is a parsing and serialisation library with zero external runtime dependencies.
The primary concerns are:

- **Denial of service** via deeply nested or pathological HUML inputs (controlled by
  `HumlOptions.MaxRecursionDepth`).
- **Unsafe deserialisation** of attacker-controlled HUML into .NET types.

Out of scope: vulnerabilities in the build toolchain, test fixtures, or documentation.
