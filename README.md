# Logo Atlas Creator

Logo Atlas Creator is a Windows desktop tool for arranging selected PNG logos in
a fixed grid and saving the result as one square, transparent PNG. The repository
is also known as the DJ Logo Wall Project. It contains the atlas-making app, not
a Unity world, material, shader, or automated logo-wall integration.

<img width="782" height="588" alt="Logo Atlas Creator window" src="https://github.com/user-attachments/assets/7fc7ab79-e417-4b83-acbf-0c7670a0229a" />

This guide describes the behavior in the checked-in source. Start with
[maintenance and validation](MAINTAINING.md) when changing the application, and
read [agent instructions](AGENTS.md) before automated work. The software is
licensed under [MIT](LICENSE.txt).

## Build and run

Use Windows and the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
The project targets `net10.0-windows` and Windows Forms; it is not a portable
Linux/macOS GUI. There is no `global.json` pinning an SDK patch version and no
third-party `PackageReference` in the project file. A Visual Studio installation
that supports this target can open `LogoAtlasApp.slnx`; the CLI can build the
project directly without solution-format support.

From the repository root:

```powershell
dotnet --list-sdks
dotnet build LogoAtlasApp/LogoAtlasApp.csproj -c Release
dotnet run --project LogoAtlasApp/LogoAtlasApp.csproj -c Release --no-build
```

`dotnet build` restores the project as needed. Build output is under
`LogoAtlasApp/bin/Release/net10.0-windows/`; `bin` and `obj` are ignored. The app
has no implemented command-line processing or batch conversion mode.

To prepare a **framework-dependent Windows x64 folder** for a separate machine:

```powershell
dotnet publish LogoAtlasApp/LogoAtlasApp.csproj -c Release -r win-x64 --self-contained false -o artifacts/publish/win-x64
```

That example requires the .NET 10 **Desktop Runtime** on the receiving machine.
Distribute the complete publish folder, not just the `.exe`. It is a packaging
example, not a claim that this repository has a tested release, installer,
single-file build, or supported architecture matrix. See Microsoft's
[publish reference](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish).

## Create an atlas

1. Click **Select PNGs** and select the input files in the file dialog. A new
   selection replaces the previous selection; it does not append to it.
2. Check the final **Columns** and **Rows** controls. Selection and grid changes
   can enlarge the grid automatically and show adjustment dialogs.
3. Choose the square output **Resolution** and **Padding** in output pixels.
   The preview updates when these controls change.
4. Click **Generate Atlas**. The app validates the available cell space, draws
   the full-resolution image, then opens a save dialog with `atlas.png` as the
   suggested filename. Choose a writable path and save.
5. Use **Open Saved** to open the last successfully saved file in the system's
   associated image viewer. The button starts disabled.

| Control | Allowed values | Initial value |
|---|---|---|
| Columns | 1 through 6 | 4 |
| Rows | 1 through 4 | 1 |
| Resolution | 1024, 2048, or 4096 pixels per side | 2048 |
| Padding | 0 through 512 pixels | 4 |
| Theme, in Settings | Light Mode or Dark Mode | Light when no readable preference exists |

The grid can hold at most **24 images**. If the selected count exceeds the final
grid capacity, generation displays an informational dialog and uses only the
first `columns × rows` paths. There is no per-image reorder, remove, or filename
list in the UI. The app uses the order returned by the file dialog; it does not
sort filenames itself. Check the preview before saving when order matters.

Automatic capacity adjustment first tries increasing rows with the current
column count, then columns with the current row count, and otherwise assigns a
6-by-4 grid. Its controls fire further adjustment events. The fallback warning
can incorrectly say a selection exceeds 24 even when it fits after both
dimensions grow. Check the **final controls** and the generation-time capacity
message rather than treating that warning text as an accurate count diagnosis.
This is a known source behavior, not a new overflow policy.

## Layout and output format

Images fill cells from left to right, then top to bottom, starting at the top-left
of the atlas. Every cell has the same inner width and height. With atlas side
`S`, columns `C`, rows `R`, and padding `P`, generation computes:

```text
available width  = S - (C + 1) * P
available height = S - (R + 1) * P
cell width      = available width / C       (integer division)
cell height     = available height / R      (integer division)
cell origin     = (P + column * (cell width + P),
                   P + row * (cell height + P))
```

Generation refuses non-positive available dimensions or cell dimensions. The
padding control's maximum alone does not guarantee a valid combination; use
less padding, a larger resolution, or fewer grid divisions if it leaves no
space. Integer-division remainder pixels remain at the right/bottom rather than
being distributed among cells.

Each complete source image is scaled by the smaller of the width and height
ratios, rounded to integer draw dimensions, and centered in its cell. This
preserves aspect ratio and can enlarge small images. Existing transparent
margins inside an input PNG count toward its dimensions; the app does not trim
them. It uses high-quality bicubic image interpolation and high-quality drawing
on a transparent bitmap. Empty cells and unused space stay transparent.

Only the PNG is written. There is no JSON/CSV manifest, sprite name table, UV
metadata, rotation, edge extrusion, automatic packing, or Unity import setup.
Consumers must keep their own input-to-cell mapping. No labels, grid borders,
cell backgrounds, or light/dark preview colors are added to the exported atlas.

The preview is advisory: it draws a smaller decorated bitmap using different
rounding and minimum-size clamps. A visible preview is not proof that generation
will accept the settings or that extremely thin image dimensions will remain
visible in the exported PNG.

## Files and saved state

The app reads the selected local images and writes the atlas only to the path
chosen in the save dialog. It has no implemented network upload or remote
storage path.

Theme changes are written as `dark` or `light` to
`%APPDATA%\LogoAtlasApp\theme.txt`. This is the only persisted app setting. A
missing file, unreadable file, or value other than `dark` selects light mode;
preference read/write failures are silently ignored. Grid settings, file
selection, the generated bitmap, and the last saved path are session-only.

**Open Saved** refers to the last successful save in this app session, which may
be older than the current selection or preview. Cancelling a save leaves the
generated bitmap in the window but does not save a new file or update that path.
It does not clear an earlier successful path. If that earlier file is deleted,
Open Saved reports it missing and disables the button.

## Troubleshooting and current limits

| Symptom | What the source does / what to check |
|---|---|
| No images selected | Generate shows `No images selected` and returns. |
| Missing thumbnail | Preview loading errors are ignored and the cell can appear empty. Confirm the file is a readable, valid PNG. Generation reloads it separately. |
| Invalid Padding or Invalid Cell Size | Reduce padding, increase output resolution, or reduce grid divisions. The advisory preview can still display an invalid combination. |
| More images than cells | Only the first paths up to final grid capacity are exported. Reselect the desired set or enlarge the grid within its limits. |
| Cannot edit/delete a selected input | Cached `Image.FromFile` objects stay alive until the next selection or form disposal. Reselect a different set or close the app to release them. |
| Open Saved cannot launch a viewer | The handler displays the launch exception. Check the file's existence and Windows PNG association. |
| Generation or saving raises an exception | Image loading, bitmap creation/drawing, and PNG saving in the export handler do not have a recovery catch. A bad image, inaccessible file, memory pressure, or unwritable destination can interrupt the operation. Preserve source images and retry with a small valid set and writable destination. |

All selected images are cached for previews, including paths beyond the 24-cell
maximum. There is no file-size limit, background worker, cancellation UI,
automatic retry, or progress indication for loading/rendering. Large or numerous
inputs can consume substantial memory and block the UI. Extremely wide/tall
images can round to a zero-sized export dimension even though preview clamps it
to one pixel; this edge case needs a source fix and runtime verification before
it can be called supported.

The file-lock lifetime follows Microsoft's
[`Image.FromFile` documentation](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.image.fromfile?view=windowsdesktop-10.0).
See [known gaps and acceptance checks](MAINTAINING.md#known-gaps-and-product-decisions)
before changing these behaviors.
