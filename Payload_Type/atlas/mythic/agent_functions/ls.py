from mythic_payloadtype_container.MythicCommandBase import *
import json


class LsArguments(TaskArguments):
    def __init__(self, command_line):
        super().__init__(command_line)
        self.args = {}

    async def parse_arguments(self):
        pass


class LsCommand(CommandBase):
    cmd = "ls"
    needs_admin = False
    help_cmd = "ls [path]"
    description = "List contents of specified directory."
    version = 1
    author = ""
    argument_class = LsArguments
    attackmapping = []
    browser_script = BrowserScript(script_name="ls", author="@its_a_feature_")

    async def create_tasking(self, task: MythicTask) -> MythicTask:
        return task

    async def process_response(self, response: AgentResponse):
        pass
