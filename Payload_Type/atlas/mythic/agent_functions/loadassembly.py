from mythic_payloadtype_container.MythicCommandBase import *
import json
import base64
import sys
from mythic_payloadtype_container.MythicRPC import *


class LoadAssemblyArguments(TaskArguments):
    def __init__(self, command_line):
        super().__init__(command_line)
        self.args = {
            "assembly_id": CommandParameter(
                name="File to Load",
                type=ParameterType.File,
                description="",
                required=False,
            )
        }

    async def parse_arguments(self):
        if len(self.command_line) > 0:
            if self.command_line[0] == "{":
                self.load_args_from_json_string(self.command_line)


class LoadAssemblyCommand(CommandBase):
    cmd = "loadassembly"
    needs_admin = False
    help_cmd = "loadassembly"
    description = "Load an arbitrary .NET assembly via Assembly.Load and track the assembly FullName to call for execution with the runassembly command. "
    version = 1
    author = ""
    argument_class = LoadAssemblyArguments
    attackmapping = []

    async def create_tasking(self, task: MythicTask) -> MythicTask:
        task.args.add_arg("remote_path", "")
        if task.args.get_arg("assembly_id") is None:
            # the user supplied an assembly name instead of uploading one, see if we can find it
            # the get_file call always returns an array of matching files limited by how many we specify
            resp = await MythicRPC().execute("get_file", task_id=task.id, filename=task.args.command_line, limit_by_callback=False)
            if resp.status == MythicStatus.Success:
                task.args.add_arg("assembly_id", resp.response[0]["agent_file_id"])
            else:
                raise Exception(
                    "Failed to find file:  {}".format(task.args.command_line)
                )
        else:
            filename = json.loads(task.original_params)["File to Load"]
            file_resp = await MythicRPC().execute("create_file", task_id=task.id,
                                                  file=base64.b64encode(task.args.get_arg("assembly_id")).decode(),
                                                  saved_file_name=filename,
                                                  delete_after_fetch=False,
                                                  )
            if file_resp.status == MythicStatus.Success:
                task.args.add_arg("assembly_id", file_resp.response["agent_file_id"])
                task.display_params = filename
            else:
                raise Exception(
                    "Failed to register file with Mythic: {}".format(file_resp.error_message)
                )

        return task

    async def process_response(self, response: AgentResponse):
        pass
