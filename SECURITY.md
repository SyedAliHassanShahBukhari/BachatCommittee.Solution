# Security Policy

## Reporting a Vulnerability
If you believe you have found a security vulnerability, please report it privately.

- Do **not** open a public GitHub issue for security findings.
- Provide enough detail to reproduce the issue (steps, affected endpoints/modules, logs if safe).
- If possible, include a suggested fix or mitigation.

## What to Include
- Description of the vulnerability
- Impact assessment (what an attacker can do)
- Steps to reproduce (PoC details if safe)
- Affected versions/branches/commit hash
- Any relevant configuration assumptions

## Scope
In scope:
- API authentication/authorization issues
- Multi-tenant data isolation issues
- Injection (SQL/command), SSRF, deserialization, file upload issues
- Secrets exposure, sensitive logging, insecure configuration defaults
- CI/CD or dependency supply-chain issues

Out of scope:
- Social engineering attacks
- Physical attacks
- Vulnerabilities requiring access to contributor machines

## Secure Development Principles (Project Conventions)
- No secrets in source control (use environment variables / user-secrets).
- Explicit repository queries; avoid string concatenation for SQL.
- Validate all external inputs at API boundaries.
- Least privilege: minimal permissions for service accounts and DB users.
- Log security-relevant events (auth failures, permission denials) without leaking secrets.

## Dependency Management
- Keep dependencies pinned via `Directory.Packages.props`.
- Regularly review and update dependencies.
- CI should fail on known critical vulnerabilities where practical.

## Disclosure Timeline
We aim to:
- Acknowledge reports promptly.
- Provide a mitigation or fix as soon as feasible.
