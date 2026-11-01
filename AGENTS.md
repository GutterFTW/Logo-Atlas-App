# Repository guidance

This is the Windows Forms Logo Atlas Creator app, also called the DJ Logo Wall
Project. It is not a Unity project. Read [README.md](README.md) for current user
behavior, build/run commands, saved state, and output limits, then
[MAINTAINING.md](MAINTAINING.md) for source navigation, acceptance checks, known
gaps, and unresolved product decisions. The actual target is defined in
`LogoAtlasApp/LogoAtlasApp.csproj` (`net10.0-windows`).

Preserve existing work and use isolated branches for changes. Keep changes
scoped; stage explicit paths and never add personal logos, generated output,
credentials, or local preferences. No dependency, target-framework, installer,
release, or Git-hook changes are implied by documentation work.

The README describes observed source behavior. Do not turn a bug, comment, or
implementation choice into a new product rule. Before changing capacity, image
order, overflow handling, export format, persistence, or distribution targets,
record the intended behavior and the approving decision's source/scope. Update
the owning docs in the same PR and retain explicit known-gap boundaries.

Build with `dotnet build LogoAtlasApp/LogoAtlasApp.csproj -c Release` on Windows
with the .NET 10 SDK. There is no checked-in automated suite or CI workflow.
Run relevant manual acceptance cases from MAINTAINING and record actual results;
source review and compilation do not establish GUI, PNG, performance, or release
acceptance. Preserve the exact managed block below when editing this file.

<!-- graphify-rules-start (managed by `graphify init`) -->
## Use Graphify before grep

This repository is indexed by Graphify: a code graph over its call, dependency, and test structure, exposed through a connected Graphify MCP server. Before reaching for grep or reading files, use the Graphify tools your MCP client lists (their exact names and descriptions are in the server's tool list) for what the graph knows and a text search does not:

- find where a symbol, function, or class is defined (instead of grepping for it)
- understand how something works, or where a behavior is handled
- find who calls a function, or what it calls
- see what a change affects (its blast radius) and which tests cover it
- map a file's dependencies and dependents

Fall back to grep or file reads only for what the graph does not model: literal string or comment matches, non-indexed files, or reading a file you have already located. If no Graphify tools are listed, check the MCP server connection.
<!-- graphify-rules-end -->
