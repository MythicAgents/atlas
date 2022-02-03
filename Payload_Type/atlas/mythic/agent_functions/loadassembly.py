from mythic_payloadtype_container.MythicCommandBase import *
import json
import base64
import sys
from mythic_payloadtype_container.MythicRPC import *


class LoadAssemblyArguments(TaskArguments):
    def __init__(self, command_line, **kwargs):
        super().__init__(command_line, **kwargs)
        self.args = [
            CommandParameter(
                name="assembly_id", cli_name="new-assembly", display_name="Assembly to upload", type=ParameterType.File,
                description="Select new file to upload",
                parameter_group_info=[
                    ParameterGroupInfo(
                        required=True,
                        group_name="Default"
                    )
                ]
            ),
            CommandParameter(
                name="filename", cli_name="assembly-name", display_name="Filename within Mythic",
                description="Supply existing filename in Mythic to upload",
                type=ParameterType.ChooseOne,
                dynamic_query_function=self.get_files,
                parameter_group_info=[
                    ParameterGroupInfo(
                        required=True,
                        group_name="specify already uploaded file by name"
                    )
                ]
            ),
        ]

    async def parse_arguments(self):
        if len(self.command_line) > 0:
            if self.command_line[0] == "{":
                self.load_args_from_json_string(self.command_line)

    async def parse_dictionary(self, dictionary):
        self.load_args_from_dictionary(dictionary)

    async def get_files(self, callback: dict) -> [str]:
        file_resp = await MythicRPC().execute("get_file", callback_id=callback["id"],
                                              limit_by_callback=False,
                                              get_contents=False,
                                              filename="",
                                              max_results=-1)
        if file_resp.status == MythicRPCStatus.Success:
            file_names = []
            for f in file_resp.response:
                if f["filename"] not in file_names:
                    file_names.append(f["filename"])
            return file_names
        else:
            return []


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
        try:
            groupName = task.args.get_parameter_group_name()
            task.args.add_arg("remote_path", value="", parameter_group_info=[ParameterGroupInfo(group_name=groupName)])
            if groupName == "Default":
                file_resp = await MythicRPC().execute("get_file",
                                                      file_id=task.args.get_arg("assembly_id"),
                                                      task_id=task.id,
                                                      get_contents=False)
                if file_resp.status == MythicRPCStatus.Success:
                    if len(file_resp.response) > 0:
                        filename = file_resp.response[0]["filename"]
                        task.display_params = filename
                    else:
                        raise Exception("Failed to locate file with Mythic")
                else:
                    raise Exception(
                        "Failed to locate file with Mythic: {}".format(file_resp.error_message)
                    )
            else:
                # the user supplied an assembly name instead of uploading one, see if we can find it
                # the get_file call always returns an array of matching files limited by how many we specify
                resp = await MythicRPC().execute("get_file", task_id=task.id, filename=task.args.get_arg("filename"), limit_by_callback=False)
                if resp.status == MythicRPCStatus.Success and len(resp.response) > 0:
                    task.args.add_arg("assembly_id", resp.response[0]["agent_file_id"])
                    task.args.remove_arg("filename")
                else:
                    raise Exception(
                        "Failed to find file:  {}".format(task.args.get_arg("filename"))
                    )

        except Exception as e:
            raise Exception("Error from Mythic: " + str(sys.exc_info()[-1].tb_lineno) + " : " + str(e))

        return task

    async def process_response(self, response: AgentResponse):
        pass
