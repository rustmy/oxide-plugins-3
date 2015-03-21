Rust:IO Oxide Plugins
=====================
An automatically updated repository for [Oxide 2](https://github.com/OxideMod/Oxide)'s [Rust Experimental](http://playrust.com) plugins.

Structure
---------
Each plugin has its own directory named after the plugin's resource id. Directories contain:

### plugin.json
A descriptor with information about the plugin:

* **id**  
  The plugin's resource id.
* **compatibility**  
  Oxide 2 version compatiblity.
* **name**  
  The plugin's name.
* **type**  
  The plugin's source type (e.g. C#/CSharp, Lua, JavaScript, Python, Bundle).
* **version**  
  The plugin's current version (no semantic versioning yet).
* **rating**  
  The average plugin rating.
* **downloads**  
  The number of times this plugin has been downloaded.
* **lastUpdate**  
  The plugin's last update time as an ISO 8601 string.
* **description**  
  The plugin's short description.
* **iconUrl**  
  The URL of the plugin's resource icon.
* **downloadUrl**  
  The URL of the plugin download.
* **filename**  
  The plugin's source file name.

### [Filename].cs/.lua/.js/.py/.zip
A file containing the plugin's latest sources.

Updating
--------
In its current state, the repository is not updated in a specific interval. We are working on providing this in the future, though.

Provided by [Rust:IO](http://playrust.io)

All plugins belong to their respective authors.
Rust:IO is not affiliated with, nor endorsed by Facepunch Studios LTD or OxideMod.
All trademarks belong to their respective owners.
