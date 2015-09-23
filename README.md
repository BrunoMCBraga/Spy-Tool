# Spy-Tool
A simple and modular set of tools capable of capturing keys, sound, screen and interact with shell of a remote infected client.


This tool was developed as a project for a colege subject called Forensic Cyber-Security and it follows the classic client-server architecture.
It's in essence a simple malware with the following functionalities:Keylogging,Remote shell,Remote Sound Capture and Remote Desktop Capture
  
The client  (infected) side is obtained by compiling the contents of the folder Keylogger/Client. The LeechActivator (tight schedule, no time for fancy names :) ) is the entrypoint of the malware that connects to the remote server (controlled by the attacker). The CPP files implement the keylogging part that uses hooks.

As for the server, the user (naughty user) should run every module individually as needed.


This tool is not intended to be used for malicious activities since for those we expect you to find a better coded malware.

We are not writting more code on this project for the foreseeable future. Yet, feel free to contribute to it.


