# CEP — Command Execution Protocol

CEP (Command Execution Protocol) defines a simple text-based message format for describing and executing command-line invocations, loosely inspired by HTTP message structure.

## Protocol Version

Current version: **CEP/0.1**

---

## Request Message

A CEP request message consists of three sections. The headers and arguments sections are separated by a blank line:

```
<Verb> <Command> <Protocol>
<Header-Name>: <Header-Value>
...

<Arguments>
```

### Start-Line

```
EXEC <command> CEP/0.1
```

| Field      | Description                                        |
|------------|----------------------------------------------------|
| `Verb`     | Action to perform. Currently only `EXEC` is defined. |
| `Command`  | Executable command name or path.                   |
| `Protocol` | Protocol token and version, e.g. `CEP/0.1`.       |

### Headers (optional)

Zero or more `Name: Value` lines, terminated by a blank line. Header names are **case-insensitive**.

| Header              | Description                                                   | Example                        |
|---------------------|---------------------------------------------------------------|--------------------------------|
| `Working-Directory` | Working directory for process execution.                      | `${USERPROFILE}\Downloads`     |
| `Timeout`           | Timeout in **seconds**. Values less than or equal to `0` mean no timeout. | `30`                           |
| `Charset`           | Encoding name for stdout/stderr.                              | `GBK`                          |

Header values support `${VAR_NAME}` placeholder expansion. During parsing, placeholders are resolved from parse-time expansion variables first, then from the host process environment. Unknown variables are kept as-is.

If a header name matches the pattern `^[A-Za-z_][A-Za-z0-9_]*$` (i.e. a valid environment variable name), the name and its value will be injected into the child process environment. This child process environment is distinct from both parse-time expansion variables and the host process environment used during placeholder expansion.

### Arguments (payload)

Each non-blank line after the header section represents one argument:

- **Token argument** — A single token on a line, passed as-is to the process argument list.
- **Named argument** — Two tokens separated by whitespace on the same line. The first token is the option name and the rest is the option value; they are added as two separate entries in the argument list.

```
-i video.mp4          ← named argument: name="-i", value="video.mp4"
-y                    ← token argument: "-y"
output.mp4            ← token argument: "output.mp4"
```

Argument values also support `${VAR_NAME}` placeholder expansion, using the same resolution order as header values.

---

## Response Message

A CEP response message is returned after command execution:

```
<Protocol> <ExitCode>
<Header-Name>: <Header-Value>
...

<Payload>
```

### Status-Line

```
CEP/0.1 <exit-code>
```

| Field      | Description                                              |
|------------|----------------------------------------------------------|
| `Protocol` | Protocol token, echoed from the request.                 |
| `ExitCode` | Integer exit code of the process (or synthetic code).    |

### Synthetic Exit Codes

| Code | Meaning    | Description                                  |
|------|------------|----------------------------------------------|
| 124  | Timeout    | Process exceeded the configured timeout.     |
| 130  | Canceled   | Execution was canceled by the caller.        |

### Response Headers

| Header              | Description                                        |
|---------------------|----------------------------------------------------|
| `Working-Directory` | Actual working directory used.                     |
| `Process-Id`        | OS process ID.                                     |
| `Start-Time`        | Process start time (ISO 8601).                     |
| `Exit-Time`         | Process exit time (ISO 8601).                      |
| `Total-Time`        | Total processor time consumed.                     |
| `User-Time`         | User-mode processor time consumed.                 |
| `Reason`            | Present on failure: `Timeout` or `Canceled`.       |

### Payload

The combined standard output and standard error of the process (when `MergeStandardOutputAndStandardError` is enabled), or standard output only (falling back to standard error if empty).

---

## Examples

### Minimal request

```
EXEC dotnet CEP/0.1

--version
```

### Request with headers

```
EXEC ffmpeg CEP/0.1
Working-Directory: ${USERPROFILE}\Downloads

-i video.mp4
-i audio.mp4
-c:v copy
-c:a aac
-map 0:v:0
-map 1:a:0
-y
output.mp4
```

### Request with charset

```
EXEC ping CEP/0.1
Charset: GBK

baidu.com
```
