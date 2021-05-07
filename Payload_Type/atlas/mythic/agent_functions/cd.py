from mythic_payloadtype_container.MythicCommandBase import *


class CdArguments(TaskArguments):
    def __init__(self, command_line):
        super().__init__(command_line)
        self.args = {}

    async def parse_arguments(self):
        pass


class CdCommand(CommandBase):
    cmd = "cd"
    needs_admin = False
    help_cmd = "cd [C:\\path\\to\\change\\to]"
    description = "Change current working directory of Atlas instance."
    version = 1
    author = "@Airzero24"
    argument_class = CdArguments
    attackmapping = []

    async def create_tasking(self, task: MythicTask) -> MythicTask:
        return task

    async def process_response(self, response: AgentResponse):
        pass
