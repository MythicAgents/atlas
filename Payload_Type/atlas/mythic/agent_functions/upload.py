from mythic_payloadtype_container.MythicCommandBase import *
import json
from mythic_payloadtype_container.MythicRPC import *


class UploadArguments(TaskArguments):
    def __init__(self, command_line, **kwargs):
        super().__init__(command_line, **kwargs)
        self.args = [
            CommandParameter(
                name="assembly_id",
                display_name="File to Upload", type=ParameterType.File, description="",
                parameter_group_info=[
                    ParameterGroupInfo(
                        required=True,
                        ui_position=1
                    )
                ]
            ),
            CommandParameter(
                name="remote_path",
                type=ParameterType.String,
                description="Take a file from the database and store it on disk through the callback.",
            ),
        ]

    async def parse_arguments(self):
        if len(self.command_line) > 0:
            if self.command_line[0] == "{":
                self.load_args_from_json_string(self.command_line)
            else:
                raise ValueError("Missing JSON argument")
        else:
            raise ValueError("Missing required parameters")

    async def parse_dictionary(self, dictionary):
        self.load_args_from_dictionary(dictionary)


class UploadCommand(CommandBase):
    cmd = "upload"
    needs_admin = False
    help_cmd = "upload"
    description = "Upload a file to the remote host"
    version = 1
    author = ""
    argument_class = UploadArguments
    attackmapping = ["T1132", "T1030"]

    async def create_tasking(self, task: MythicTask) -> MythicTask:
        filename = json.loads(task.original_params)["File to Upload"]
        file_resp = await MythicRPC().execute("create_file", task_id=task.id,
                                              file=base64.b64encode(task.args.get_arg("assembly_id")).decode(),
                                              saved_file_name=filename,
                                              delete_after_fetch=False,
                                              )
        if file_resp.status == MythicStatus.Success:
            task.args.add_arg("assembly_id", file_resp.response["agent_file_id"])
            if task.args.get_arg("remote_path") == "":
                task.args.add_arg("remote_path", filename)
            elif task.args.get_arg("remote_path")[-1] == "\\":
                task.args.add_arg("remotepath", task.args.get_arg("remote_path") + filename)
            task.display_params = filename + " to " + task.args.get_arg("remote_path")
        else:
            raise Exception(
                "Failed to register file with Mythic: {}".format(file_resp.error_message)
            )
        return task

    async def process_response(self, response: AgentResponse):
        pass
