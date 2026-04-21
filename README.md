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

### CLI Options

- `-t`, `--text`: CEP message text input.
- `-f`, `--file`: CEP message file path input.
- `-l`, `--log`: write request/response logs under `./logs`.
- `-e`, `--env`: expansion variable in `KEY=VALUE` format. Repeatable.
- `--env-file`: env file path for expansion variables in `KEY=VALUE` format.

`--env-file` parsing rules:

- Empty lines are ignored.
- Lines starting with `#` are treated as comments.
- Every non-empty line must be `KEY=VALUE`.

When the same key exists in both `--env-file` and `--env`, `--env` overrides `--env-file`.

Examples:

```powershell
.\cclient.exe run --file request.cep --env-file .env
.\cclient.exe run --file request.cep -e USER=alice -e REGION=cn
.\cclient.exe run --file request.cep --env-file .env -e USER=bob
```

## CEP Message Overview

A request has three logical sections:

1. Start-Line: `<Verb> <Command> <Protocol>`
2. Headers (optional): `Name: Value`
3. Arguments (payload): one argument per non-blank line

Headers and arguments are separated by one blank line.

### Common Request Headers

- `Working-Directory`: working directory for process execution
- `Timeout`: timeout in seconds, where values less than or equal to `0` mean no timeout
- `Charset`: stdout/stderr encoding name

Header values and argument values support `${VAR_NAME}` expansion. During parsing, placeholders are resolved from expansion variables first, then from the host process environment. If a header name matches `^[A-Za-z_][A-Za-z0-9_]*$`, it is injected into the child process environment. These are separate concepts: expansion variables are parse-time inputs, while the child process environment is used only when launching the command.

### Response Essentials

- Status line: `<Protocol> <ExitCode>`
- Success and failure metadata are returned in headers
- `Reason` header is present for timeout/cancel cases
- Synthetic exit codes:
	- `124` = timeout
	- `130` = canceled

## Notes

For the precise grammar and behavior details, always treat [CEP.md](CEP.md) as the source of truth.
