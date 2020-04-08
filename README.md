# AsInvoker

AsInvoker is a Windows executable "de-escalator" - it removes any administrator
privilege requirement specified by the executable in its manifest.

## Usage

Usage: `AsInvoker.exe exe_to_deescalate.exe`

(Or in explorer, drag the target executable onto AsInvoker.)

AsInvoker works by using resource APIs from `kernel32.dll`. It reads application manifests from executable images,
searches them for `requestedExecutionLevel` elements, and replaces any elevation requests.

More on `requestedExecutionLevel` can be found [here](https://docs.microsoft.com/en-us/previous-versions/bb756929(v=msdn.10)).

## Example

We demonstrate AsInvoker with [Disk2vhd](https://docs.microsoft.com/en-us/sysinternals/downloads/disk2vhd),
a Sysinternals utility that creates VHD images for disk volumes.
To do this, it requests administrator privileges in its manifest.

When opened, a UAC prompt pops up. The utility runs normally.

![Normal](/Readme/Images/disk2vhd_normal.png)

Now we process Disk2vhd with AsInvoker. The UAC shield icon disappears
from the icon of the utility.

When opened, the UAC prompt no longer pops up. Also, the utility now
is unable to read the list of disk volumes due to the lack of privileges.

![Patched](/Readme/Images/disk2vhd_patched.png)

## Caveats

When viewed with Resource Hacker, the original manifest actually still exists in the modified application.
This means the executable will have two manifest resources.
However, the patched manifest comes before the original one, which I suspect is why it still works.

I have yet to find a way to remove the original manifest - calling `UpdateResource` with `lpData` and `cb`
set to zero will throw an `ERROR_INVALID_PARAMETER` and prevent me from inserting the new manifest or
saving changes.
