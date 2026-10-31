# Maintaining Logo Atlas Creator

## Authority and scope

The [README](README.md) owns the current user workflow, build commands, output
format, and operational limits. This file owns source navigation and validation.
The [project file](LogoAtlasApp/LogoAtlasApp.csproj) owns the actual target and
build settings. If source and documentation disagree, investigate and correct
the discrepancy; do not use a new implementation detail as an unapproved product
requirement. Preserve the managed Graphify section in [AGENTS.md](AGENTS.md).

The current implementation is one Windows Forms application. There is no Unity
project, external service, database, plugin framework, dependency lockfile,
automated test project, or checked-in CI workflow. The filename
`LogoAtlasApp.slnx` lists the one C# project. A prebuilt binary from another
folder or an earlier release is not evidence for this branch.

## Source map

| File / symbol | Responsibility |
|---|---|
| `LogoAtlasApp/Program.cs`, `Main` | STA entry point, Windows Forms initialization, launches `MainForm`. |
| `LogoAtlasApp/LogoAtlasApp.csproj` | `WinExe`, `net10.0-windows`, Windows Forms, nullable and implicit usings; no explicit package dependencies. |
| `MainForm` constructor | Controls, ranges/defaults, tooltips, event wiring, initial theme and preview. There is no separate designer file. |
| `BtnSelect_Click` | Replaces file paths, disposes the old cache, loads the new preview cache, adjusts grid capacity, redraws. |
| `EnsureGridCapacity` | Rows-first/columns-second adjustment and maximum-grid fallback. Numeric control changes re-enter this through `ValueChanged`. |
| `UpdatePreviewGrid` | Small advisory bitmap, theme-specific decoration, cached thumbnails, minimum-size clamps, replacement of the PictureBox image. |
| `BtnGenerate_Click` | Export validation, capacity truncation, full-resolution layout/drawing, save dialog, last-saved path. |
| `BtnOpenSaved_Click` | Session path/existence checks and shell launch with `UseShellExecute = true`. |
| `LoadImagesToCache`, `DisposeCachedImages`, `Dispose` | Cached image lifetime and disposal when reselecting/closing. |
| `SetTheme`, `ApplyTheme` | Theme persistence, checkmarks/colors, then preview redraw. |
| `GetSettingsPath`, `LoadThemePreference`, `SaveThemePreference` | Roaming AppData `LogoAtlasApp/theme.txt`; best-effort text setting. |
| `LogoAtlasApp/icon.png` | Tracked image asset. The current project does not declare it as `ApplicationIcon`. |

All form methods above are in
[`LogoAtlasApp/MainForm.cs`](LogoAtlasApp/MainForm.cs). Query Graphify to locate
symbols and relationships before source navigation, then read the located file
at the branch being changed. The indexed snapshot can lag a PR.

## Change workflow

1. Verify branch, origin, current PR head, and worktree status. Preserve unrelated
   edits and use an isolated checkout for concurrent work.
2. Read the README, this source map, the project settings, and the actual affected
   handlers. Layout, preview, export, cache lifetime, and UI event recursion are
   related even though they share one file.
3. Write down the intended observable change. Confirm product choices that alter
   capacity, image order, overflow behavior, accepted formats, export metadata,
   persistence, or distribution targets before implementing them. Record the
   decision's source and scope; label an unconfirmed proposal as a proposal.
4. Change the smallest relevant path and update owning documentation in the same
   PR. Preserve selected-path/index alignment, transparent export, source-image
   aspect ratio, and disposal unless an approved behavior change says otherwise.
5. Build with the documented project command, inspect the diff and link targets,
   and run the applicable manual cases below on Windows. Record the exact SDK,
   Windows version, commit, inputs, settings, expected/actual result, and evidence.
6. Obtain review of the pushed revision and satisfy required checks. Compilation,
   static review, and GUI/output acceptance are different results.

Do not add generated `bin`, `obj`, publish folders, local preferences, or personal
input logos to a documentation or source commit. Do not silently install/enable
Git hooks or change target framework/dependencies as a documentation side effect.

## Acceptance checks

There is no automated suite in the repository. `dotnet test` alone is not
evidence that the GUI or exported PNG works. These are reproducible manual cases,
not a statement that they have passed. Use expendable input copies and output
paths; do not overwrite artwork during validation.

| ID | Setup and action | Expected observable result / evidence |
|---|---|---|
| LA-01 | Launch a new process with no readable theme file. | Light mode; columns 4, rows 1, resolution 2048, padding 4; Open Saved disabled. Record screenshot and environment. |
| LA-02 | Generate before selecting files, then cancel a selection dialog. | No-images message on Generate; cancelling selection keeps the previous selection (empty in this case). |
| LA-03 | Select four distinguishable, valid PNGs of mixed aspect ratios, including transparency; use 4×1, 2048, padding 4; save. | PNG is 2048×2048; row-major image order matches the path list returned by the dialog, aspect ratios are retained, surrounding space is transparent, and preview grid decoration is absent. Inspect actual PNG pixels/dimensions, not only the preview. |
| LA-04 | Change columns so the selected set no longer fits; record controls and dialogs after handlers settle. | Source attempts rows first, then columns, then the maximum grid; observe the known misleading fallback warning below rather than assuming all warnings are accurate. |
| LA-05 | Select 25 distinguishable PNGs and generate with final 6×4 controls. | Capacity message; output uses first 24 paths. The 25th is not exported. Record final controls because selection adjustment can show repeated dialogs. |
| LA-06 | Set resolution 1024 and padding 512 with a 1×1 grid; Generate. | Invalid-padding error and no save dialog. A visible preview does not make export valid. Restore normal settings afterwards. |
| LA-07 | Save file A; change selection/settings; generate again and cancel saving. | Generated preview changes, A remains the last successful saved path, and Open Saved still opens A. Restarting the app loses this path. |
| LA-08 | Delete a previously saved expendable file, then Open Saved; separately test a viewer-launch failure in a disposable environment. | Missing-file message disables the button; a caught launch failure displays its exception. Do not change global PNG associations just to test this case. |
| LA-09 | Select a disposable text file renamed to `.png` to exercise invalid-image loading. For a missing-file case, select that invalid fixture, delete it after its suppressed preview-load failure, then Generate. Separately use extreme aspect-ratio PNGs. | Record actual preview/export failure behavior. Preview errors are suppressed; export failures are not covered by an application recovery catch. These are known gaps, not guaranteed graceful-recovery cases. |
| LA-10 | Select a large set; replace it with a small set and close the app. Try editing an input while selected and after releasing it. | Inspect responsiveness, memory, and file-lock release; no bounded performance target is currently established. |
| LA-11 | Change theme, restart, and inspect `%APPDATA%\LogoAtlasApp\theme.txt`. Test unreadable preference using an isolated Windows profile. | Theme persists when writable; unreadable load defaults light. Grid, input paths and last-saved path do not persist. Do not change the user's normal AppData permissions for a test. |

For geometry regressions, use the README equations to calculate expected cell
positions independently, including remainder pixels and transparent source
margins. Include 1×1 and 6×4 grids, 0 padding, each resolution, empty trailing
cells, and square/wide/tall logos. Test PNG alpha with an image inspector rather
than relying on a viewer's background color. Any future automated renderer tests
should exercise the actual implementation and be added with the related source
change; this documentation update does not introduce a second renderer.

## Known gaps and product decisions

These are source findings and unresolved decisions, not approved requirements or
features implemented by this documentation change:

- **Capacity warning correctness:** `EnsureGridCapacity` can reach its maximum
  fallback when neither individual dimension alone fits, even for a count at or
  below 24. For example, selecting 17 files from the initial 4×1 state can grow
  both dimensions and still display the "exceed maximum" warning. Nested
  `ValueChanged` calls can produce multiple dialogs. The README documents the
  current behavior; a later source fix should preserve/confirm intended adjustment
  policy and validate the event sequence.
- **Input and export errors:** preview image-load failures are suppressed, while
  export loading/drawing/saving has no encompassing recovery catch. Partial
  bitmap-load failures can bypass the later disposal loop. The intended policy
  for skipping a failed input versus stopping the entire export needs a product
  decision before changing outcomes.
- **Extreme geometry:** export rounds draw dimensions without the preview's
  one-pixel clamps. Extremely thin images can round to zero. Desired minimum-size
  behavior must be specified with the affected visual cases before it is called
  supported.
- **Distribution:** the project target is known, but supported Windows versions,
  CPU architectures, installation method, signing, and a release-validation
  matrix are not recorded. The README's x64 publish command is an explicit example,
  not an approved distribution policy.
- **Consumer mapping:** current output is only a PNG. Consumers must maintain
  their own cell mapping. Adding names/UV metadata, reordering, source trimming,
  or new image formats would require an agreed output/workflow contract.

## Documentation validation evidence

On 2026-10-31, documentation was reconciled against source at PR head
`59534f30f6a1fd8697d0aec1f582555fc022e79c` (the source files match base
`1abd2a927830122f395ba78070b97649d926ae2a`). The managed Graphify block was
preserved. `dotnet build LogoAtlasApp/LogoAtlasApp.csproj -c Release --nologo`
passed on Windows using SDK 10.0.401, with 0 warnings and 0 errors. Documentation
path/link and managed-block checks and `git diff --check` also passed.

These results establish source/document coherence and compilation. The manual
GUI, saved PNG, performance, and distribution cases above remain **NOT RUN**
unless a later evidence entry records their execution. No application launch or
release was performed during this documentation pass.
