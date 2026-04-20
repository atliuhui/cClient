# cClient

cClient is a CLI and .NET library for executing commands through CEP (Command Execution Protocol) messages.

## What Is CEP

CEP is a text wire format for command execution requests and responses.

- Full specification: [CEP.md](CEP.md)
- Current version: `CEP/0.1`

## Usage

After building the cClient project, the executable is generated as `cclient.exe`.

### Run With A CEP File

```powershell
.\cclient.exe run --file dotnet-request.cep
```

### Run With Logging Enabled

```powershell
.\cclient.exe run --file dotnet-request.cep --log
```

When `--log` (or `-l`) is enabled, cClient writes request/response logs to the `logs` directory under the current working directory. By default, logging is disabled.

### Run With Inline CEP Text

```powershell
.\cclient.exe run --text "EXEC dotnet CEP/0.1

--version"
```

## CEP Message Overview

A request has three logical sections:

1. Start-Line: `<Verb> <Command> <Protocol>`
2. Headers (optional): `Name: Value`
3. Arguments (payload): one argument per non-blank line

Headers and arguments are separated by one blank line.

### Common Request Headers

- `Working-Directory`: working directory for process execution
- `Timeout`: timeout in seconds
- `Charset`: stdout/stderr encoding name

Header values support `${VAR_NAME}` expansion. If a header name matches `^[A-Za-z_][A-Za-z0-9_]*$`, it is injected into the child process environment.

### Response Essentials

- Status line: `<Protocol> <ExitCode>`
- Success and failure metadata are returned in headers
- `Reason` header is present for timeout/cancel cases
- Synthetic exit codes:
	- `124` = timeout
	- `130` = canceled

## Notes

For the precise grammar and behavior details, always treat [CEP.md](CEP.md) as the source of truth.
