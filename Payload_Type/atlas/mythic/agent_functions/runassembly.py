from mythic_payloadtype_container.MythicCommandBase import *
import json
from mythic_payloadtype_container.MythicRPC import *


class RunAssemblyArguments(TaskArguments):
    def __init__(self, command_line, **kwargs):
        super().__init__(command_line, **kwargs)
        self.args = [
            CommandParameter(name="assembly_id", cli_name="assembly_name",
                             display_name="Loaded Assembly Name",
                             type=ParameterType.String, description="Name of the loaded assembly to run",
                             parameter_group_info=[
                                 ParameterGroupInfo(
                                     required=True,
                                     ui_position=1
                                 )
                             ]
            ),
            CommandParameter(
                name="args", type=ParameterType.String, display_name="Assembly arguments",
                parameter_group_info=[
                    ParameterGroupInfo(
                        required=False,
                    )
                ]
            ),
        ]

    async def parse_arguments(self):
        if len(self.command_line) > 0:
            try:
                self.load_args_from_json_string(self.command_line)
            except:
                pieces = self.command_line.split(" ")
                self.add_arg("assembly_id", pieces[0])
                self.add_arg("args", " ".join(pieces[1:]))
        else:
            raise Exception("Missing required arguments")

    async def parse_dictionary(self, dictionary):
        self.load_args_from_dictionary(dictionary)


class RunAssemblyCommand(CommandBase):
    cmd = "runassembly"
    needs_admin = False
    help_cmd = "runassembly [filename] [assembly arguments]"
    description = "Execute the entrypoint of a assembly loaded by the loadassembly command and redirect the console output back to the Apfell server."
    version = 1
    author = ""
    argument_class = RunAssemblyArguments
    attackmapping = []

    async def create_tasking(self, task: MythicTask) -> MythicTask:
        # the get_file call always returns an array of matching files limited by how many we specify
        resp = await MythicRPC().execute("get_file", task_id=task.id, filename=task.args.get_arg("assembly_id"))
        if resp.status == MythicRPCStatus.Success and len(resp.response) > 0:
            args = task.args.get_arg("args")
            if args is None:
                args = ""
            task.display_params = task.args.get_arg("assembly_id") + " " + args
            task.args.add_arg("assembly_id", resp.response[0]["agent_file_id"])
        else:
            raise Exception(
                "Failed to find file:  {}".format(task.args.command_line)
            )
        return task

    async def process_response(self, response: AgentResponse):
        pass
