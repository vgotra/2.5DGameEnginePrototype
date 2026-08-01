# Known Issues

- Build and package restore have not yet been run because the desktop shell runner currently fails to launch PowerShell.
- External package versions require validation against the installed .NET 10 SDK and Vulkan SDK.
- The current JobSystem is a provisional queue-based scheduler and does not yet implement dependency graphs or work stealing.
