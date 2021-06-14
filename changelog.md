## 5 0 14 -fix

* Updated to target .NET 4.6.1 to allow connecting again, on both Windows 7 and 10.
* Added option to turn animation of eicons on/off (to prevent them from using excessive memory and causing a crash). (from https://github.com/Axaia/slimCat)
* A fix for creating folders and logs for channels ending in trailing spaces. (from https://github.com/Hunter4242/slimCat)

## 5 0 14

Updated chat url to connect correctly.

## 5 013

Version bump as an attempt to make windows defender happy.

## 5 012

Fixes issues introduced in 011:

* Fixes alt up/down and other channel switching shortcuts not working
* Fixes a soft crash relating to all friend-request handling logic (sending/receiving/accepting/denying)

## 5 011

* Fixes a random crash due to a bug in the friend-request handling logic
* Fixes a random crash when receiving a message (mostly when the client was closed/minimized)
* Fixes an incorrect profile being opened when clicking on a profiles name (particularly if the profile was offline)

## 5 010

Adds support for animated EIcons.

## 5 009

Uses better logic for better handling a large volume of start up changes; should fix most/all startup crashes.

## 5 008

Crash fixes another startup crash issue.

## 5 007

Crash fixes a problem noticed when joining several channels at once at startup.

## 5 006

Introduced better error handling to capture errors that slimCat was unable to prior.

## 5 005

Last build before better error handling was introduced. Unstable.

## 4 11

Last build before the auto updating mechanism and move to C# 6.
