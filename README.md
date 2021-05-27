# Atlas

Atlas is a .NET 3.5 and .NET 4.0 compatible agent that is designed as an initial payload offering only the bare essentials before loading up a more fully-featured agent. This payload is updated for Mythic 2.2 and will update as necessary. It is not compatible with Mythic 2.1 and below.

The agent has `mythic_payloadtype_container==0.0.42` PyPi package installed and reports to Mythic as version "6".

## How to install an agent in this format within Mythic

When it's time for you to test out your install or for another user to install your agent, it's pretty simple. Within Mythic is a `install_agent_from_github.sh` script (https://github.com/its-a-feature/Mythic/blob/master/install_agent_from_github.sh). You can run this in one of two ways:

* `sudo ./install_agent_from_github.sh https://github.com/user/repo` to install the main branch
* `sudo ./install_agent_from_github.sh https://github.com/user/repo branchname` to install a specific branch of that repo

Now, you might be wondering _when_ should you or a user do this to properly add your agent to their Mythic instance. There's no wrong answer here, just depends on your preference. The three options are:

* Mythic is already up and going, then you can run the install script and just direct that agent's containers to start (i.e. `sudo ./start_payload_types.sh agentName` and if that agent has its own special C2 containers, you'll need to start them too via `sudo ./start_c2_profiles.sh c2profileName`).
* Mythic is already up and going, but you want to minimize your steps, you can just install the agent and run `sudo ./start_mythic.sh`. That script will first _stop_ all of your containers, then start everything back up again. This will also bring in the new agent you just installed.
* Mythic isn't running, you can install the script and just run `sudo ./start_mythic.sh`. 

