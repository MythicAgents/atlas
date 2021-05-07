from mythic_payloadtype_container.MythicCommandBase import *
import json


class ListLoadedArguments(TaskArguments):
    def __init__(self, command_line):
        super().__init__(command_line)
        self.args = {}

    async def parse_arguments(self):
        pass


class ListLoadedCommand(CommandBase):
    cmd = "listloaded"
    needs_admin = False
    help_cmd = "listloaded"
    description = (
        "Retrieve a list of .NET assemblies loaded via the loadassembly command. "
    )
    version = 1
    author = ""
    argument_class = ListLoadedArguments
    attackmapping = []

    async def create_tasking(self, task: MythicTask) -> MythicTask:
        return task

    async def process_response(self, response: AgentResponse):
        pass
