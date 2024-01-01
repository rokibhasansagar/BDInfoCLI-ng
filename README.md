# BDInfoCLI-ng

> Modified from: https://github.com/Ethan930717/BDInfoCLI-ng/tree/kimoji-bdinfo

Original source origin: http://www.cinemasquid.com/blu-ray/tools/bdinfo

BDInfoCLI-ng forked from: https://github.com/UniqProject/BDInfo

Additional sources from BDInfoCLI (https://github.com/Tripplesixty/BDInfoCLI)

BDInfoCLI-ng is the latest BDInfo (with UHD support) modified for use as a CLI utility. BDInfoCLI-ng implements an interface similar to BDInfoCLI, but on the latest BDInfo code base and with code changes designed to be as minimally invasive as possible for easier maintainability with BDInfo updates.

## Usage

```
Usage: BDInfo.exe <BD_PATH> [REPORT_DEST]
BD_PATH may be a directory containing a BDMV folder or a BluRay ISO file.
REPORT_DEST is the folder the BDInfo report is to be written to. If not
given, the report will be written to BD_PATH. REPORT_DEST is required if
BD_PATH is an ISO file.

  -?, --help, -h             Print out the options.
  -l, --list                 Print the list of playlists.
  -m, --mpls=VALUE           Comma separated list of playlists to scan.
  -w, --whole                Scan whole disc - every playlist.
  -v, --version              Print the version.
```

### Examples

```
# Display playlists in given disc, prompt user to select playlists
# to scan, and output the generated report to the same disc path:
# (If an ISO file is given, then a REPORT_DEST must be given as well. See next example.)
BDInfo.exe BD_PATH

# Same as above, but output report in given report folder:
# (If BD_PATH is an ISO, these are the minimum arguments required)
BDInfo.exe BD_PATH REPORT_OUTPUT_DIR

# Just display the list of playlists in the given disc:
BDInfo.exe -l BD_PATH

# Scan the whole disc (every playlist) and write report to disc folder (non-interactive):
BDInfo.exe -w BD_PATH

# Scan selected playlists and write report to disc folder (non-interactive):
BDInfo.exe -m 00006.MPLS,00009.MPLS BD_PATH

# Display the BDInfo version this build of BDInfoCLI-ng is based on:
BDInfo.exe -v
```

## Windows

### Requirements

<ul>
<li>Windows Vista, Windows 7 or higher Operating System</li>
<li>.NET Framework 4.5 or Higher</li>
<li>Source Code</li>
</ul>

BDInfoCLI-ng can be built using the free tool <a href="https://www.visualstudio.com/vs/community/">Microsoft Visual Studio Community Edition</a>. Just install Visual Studio, open ```BDInfo.sln```, and build.

## Linux

BDInfoCLI-ng can be built and run with <a href="https://www.mono-project.com/">Mono</a>.

Using Docker is highly recommend (nobody should have to taint their OS with Mono).

To do so install Docker and then simply use the included ``bdinfo`` wrapper script inside the ``scripts`` directory. The wrapper script automatically handles mounting the necessary directories into the container. The first run will be slow as the container image will have to be downloaded, subsequent runs will not be.

Wrapper script example:
```
./bdinfo --help

# Display playlists in given disc, prompt user to select playlists
# to scan, and output the generated report to the same disc path:
# (If an ISO file is given, then a REPORT_DEST must be given as well. See next example.)
./bdinfo BD_PATH

# Same as above, but output report in given report folder:
# (If BD_PATH is an ISO, these are the minimum arguments required)
./bdinfo BD_PATH REPORT_OUTPUT_DIR
```

Alternatively, you can run the Docker container yourself, e.g:
```
docker run --rm -it -v <BD_PATH>:/mnt/bd fr3akyphantom/bdinfocli-ng /mnt/bd
```
or, with a ``REPORT_DEST``:
```
docker run --rm -it -v <BD_PATH>:/mnt/bd -v <REPORT_DEST>:/mnt/report fr3akyphantom/bdinfocli-ng /mnt/bd /mnt/report
```

Without Docker you will need to build it and run it yourself with Mono (see the Dockerfile for details on how that's done).

## Mac

The above instructions for using BDInfoCLI-ng with Docker on Linux should also work for Macs, but it has not been tested.
