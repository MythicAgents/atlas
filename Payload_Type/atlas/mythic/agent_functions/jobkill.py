from mythic_payloadtype_container.MythicCommandBase import *
import json


class JobKillArguments(TaskArguments):
    def __init__(self, command_line, **kwargs):
        super().__init__(command_line, **kwargs)
        self.args = [
            CommandParameter(
                name="job_id",
                type=ParameterType.String,
                description="The Job Id for the running job to be killed",
                parameter_group_info=[
                    ParameterGroupInfo(
                        required=True,
                    )
                ]
            )
        ]

    async def parse_arguments(self):
        self.add_arg("job_id", self.command_line)

    async def parse_dictionary(self, dictionary):
        self.load_args_from_dictionary(dictionary)


class JobKillCommand(CommandBase):
    cmd = "jobkill"
    needs_admin = False
    help_cmd = "jobkill [job id]"
    description = "Kills a running job and removes it from the atlas instance's list of running jobs."
    version = 1
    author = ""
    argument_class = JobKillArguments
    attackmapping = []

    async def create_tasking(self, task: MythicTask) -> MythicTask:
        task.display_params = task.args.get_arg("job_id")
        return task

    async def process_response(self, response: AgentResponse):
        pass
