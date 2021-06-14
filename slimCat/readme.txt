slimCat 5.00 dev build (with update to be able to connect)
======================

This is a short update to fix slimCat being unable to connect. It also adds the option to stop eicons from animating to avoid crashes from excessive memory use. (You can turn it on/off in Settings -> Appearance)

You can use this as a fresh install, or copy it over your previous one to update it. You may want to make a copy of the original first as a safety precaution, particularly if you're using portable mode (as your settings are in there).

Settings and logs should normally be located in: %AppData%\slimCat

Portable mode settings can be in two different locations:
	Release builds (like this one): slimCat\client\logs\
	Debug builds: slimCat\logs\

If you have trouble with settings vanishing, you may want to try both locations. Specifically, channels, NotInterested and so on are in:
	slimCat\client\logs\<NAME>\Global\!settings.xml

(These things have not been changed, this is just a clarification.)



slimCat is a 3rd party desktop application for the f-chat protocol. It connects to the same server as webclients, but does so much more, such as saving your logs for you, and automatically reposting your ads.

Installing and Using
====================

Simply extract the contents into any folder. Run slimCat.exe to start the program. slimCat will stick its settings in a special folder on your computer - if you intend to share settings with another computer, or have your logs located on a thumbdrive or dropbox or similar, run the "portable mode" shortcut and then put slimCat where you want your logs and settings to be saved. slimCat will save all logs and settings relative to the .exes instead of in those special folders while running in portable mode.

Upgrading from 4.xx
===================

5.xx marks a new major version, and with it, breaking changes. slimCat now has a different folder structure. Your shortcuts will still work the same, but now sounds and themes are located in /client/sounds and /client/theme, respectively. Do not run client.exe directly.

This means you should probably just delete your old slimCat install and reinstall the theme on top of the 5.xx client folder, if you do not like the default theme.

slimCat doesn't start!
======================

If after extracting and double-left clicking slimCat.exe the program does not start, or starts and immediately closes, you should make sure windows smartscreen didn't block the program. To unblock slimCat, go into the client folder, right-click on client.exe, go to properties, and click the unblock button near the bottom. You may have to do this with slimCat.exe in the root folder as well. After doing such, the client still doesn't run, you may have to update your .NET to at least 4.6.1.

Minimum Requirements
====================

Windows 7 and .NET 4.6.1