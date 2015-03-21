Oxide 2 Plugins for Rust
========================
An automatically updated repository for [Oxide 2](https://github.com/OxideMod/Oxide)'s [Rust Experimental](http://playrust.com) plugins.

Structure
---------
Each plugin has its own directory named after the plugin's resource id. Directories contain:

#### plugin.json
A descriptor with information about the plugin:

* **id**  
  The plugin's resource id.
* **compat**  
  Oxide version compatiblity.
* **game**  
  The game this plugin is for (e.g. rust).
* **name**  
  The plugin's name.
* **category**  
  The plugin's category.
* **type**  
  The plugin's type (e.g. cs, lua, js, py, zip).
* **version**  
  The plugin's most recent version (no semantic versioning yet).
* **author**  
  Oxide user name of the plugin's author.
* **rating**  
  The average plugin rating.
* **downloads**  
  The number of times this plugin has been downloaded.
* **updated**  
  The plugin's last update time as an ISO 8601 string.
* **description**  
  The plugin's short description.
* **iconUrl**  
  The URL to the plugin's resource icon, if any.
* **downloadUrl**  
  The URL of the plugin download.
* **filename**  
  The plugin's source file name.

#### plugin.png
The plugin's resource icon, if any. This might potentially also be a JPEG with a PNG extension.

#### [filename.cs/.lua/.js/.py/.zip]
A file containing the plugin's latest sources.

Updating
--------
In its current state, the repository is not updated in a specific interval. We are working on providing this in the future, though.

All plugins belong to their respective authors.
Rust:IO is not affiliated with, nor endorsed by Facepunch Studios LTD or OxideMod.
All trademarks belong to their respective owners.
