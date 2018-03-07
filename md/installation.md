# Installation #

*<small>Last update of this page : 25/02/2018</small>*

## Install Notepad++ ##

**3P** is a notepad++ plug-in, hence the first step is to download and install the latest version notepad++ at :
[notepad-plus-plus.org/download/](https://notepad-plus-plus.org/download/).

## Install the required .net framework version ##

3P is developped in C# with the [.net framework](https://docs.microsoft.com/en-us/dotnet/framework/get-started/overview), you will need the [4.6.2 version](https://www.microsoft.com/en-us/download/details.aspx?id=53344) or superior otherwise it will simply not work!

If you don't know which version you currently have, no worries, just follow the next step. I have included a small program (NetFrameworkChecker.exe [opensource here](https://github.com/jcaillon/NetFrameworkChecker)) that can check if you fullfill the requirement.

## Installation of 3P ##

### Automatic with the plugin manager ###

*The plugin manager is no longer installed with notepad++ but you can download and install the latest version [here](https://github.com/bruderstein/nppPluginManager/releases)*.

Automatically install 3P through the plugin manager of notepad++ :

* `PLUGINS > PLUGIN MANAGER > SHOW PLUGIN MANAGER`
* Look for **3P** in the available list of plugins, check it and press install :

![image](content_images/installation/plugin_manager.png)

* The program `NetFrameworkChecker.exe` will be started to check that you have the required .net version (it will not even show if you have the required version)
* Choose `YES` when asked to restart notepad++ and allow the program `updated/gpup.exe` to be executed if windows warns you

![image](content_images/installation/warning.png)

### Manual installation of 3P ###

* Stop notepad++
* Download the [latest version](https://github.com/jcaillon/3P/releases/latest) of 3P (direct download button available on the menu on your right)
* Go to your notepad++ installation folder (usually %programfiles%\Notepad++), you should see a `/plugins/` folder
* Unzip the content of the downloaded package into the aforementioned folder (`/plugins/3P.dll` should now exist)
* Optionally, you can execute `NetFrameworkChecker.exe` to check if you have the required .net version to run 3P
* start notepad++
