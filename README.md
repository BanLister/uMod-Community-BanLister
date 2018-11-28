# Version 1.0

# Ban Lister

Ban Lister gathers bans from trusted secure servers and compiles a free to use public repository.

This plugin incorporates your communitya key ```provided by BanLister.com``` and connects your server to our ever growing BanList. As well providing the same benefits as our other Rust plugin.

### BanLister Community Key

If your looking to get a Community Key, visit [Ban Lister](https://BanLister.com/rip)and submit a Forum.

### Installation

Download the ZIP file and extract ```BanLister.cs``` addon to the addons folder.

### Configuration

Open ```BanLister.json``` from the Oxide Config directory and modify the following fields as follows.
```
"API Key": "", -- Provided BanLister Community Key
"Ban Threshold": 15, -- If the user has equal to or more recorded bans then this value, then the user will be kicked from the server.
"Kick player from server if bans exceed the threshold": true -- if false, instead of kicking users for MaxBans a message will be sent to staff. If true a message won't be sent and the user will be kicked.
```

### Security

Our developers are committed to ensuring a full working and secure plugin. This plugin connects securely to our database both inserting and retrieving data when necessary. If you have further safety concerns or general questions your more then welcome to come and chat to a developer [Discord](https://BanLister.com/Discord)
