from mythic_payloadtype_container.MythicCommandBase import *
from mythic_payloadtype_container.PayloadBuilder import *
import uuid
import asyncio
import os
from distutils.dir_util import copy_tree
import tempfile
import json


class Atlas(PayloadType):

    name = "atlas"
    file_extension = "exe"
    author = "@Airzero24"
    supported_os = [SupportedOS.Windows]
    wrapper = False
    wrapped_payloads = ["service_wrapper"]
    note = """This payload uses C# to target Windows hosts with the .NET framework installed. For more information and a more detailed README, check out: https://github.com/airzero24/Atlas"""
    supports_dynamic_loading = False
    build_parameters = {
        "version": BuildParameter(
            name="version",
            parameter_type=BuildParameterType.ChooseOne,
            description="Choose a target .NET Framework",
            choices=["4.0", "3.5"],
        ),
        "arch": BuildParameter(
            name="arch",
            parameter_type=BuildParameterType.ChooseOne,
            choices=["x64", "x86"],
            default_value="x64",
            description="Target architecture",
        ),
        "chunk_size": BuildParameter(
            name="chunk_size",
            parameter_type=BuildParameterType.String,
            default_value="512000",
            description="Provide a chunk size for large files",
            required=True,
        ),
        "cert_pinning": BuildParameter(
            name="cert_pinning",
            parameter_type=BuildParameterType.ChooseOne,
            choices=["false", "true"],
            default_value="false",
            required=False,
            description="Require Certificate Pinning",
        ),
        "default_proxy": BuildParameter(
            name="default_proxy",
            parameter_type=BuildParameterType.ChooseOne,
            choices=["true", "false"],
            default_value="true",
            required=False,
            description="Use the default proxy on the system",
        ),
        "output_type": BuildParameter(
            name="output_type",
            parameter_type=BuildParameterType.ChooseOne,
            choices=["WinExe", "Raw"],
            default_value="WinExe",
            description="Output as an EXE or Raw shellcode from Donut",
        ),
    }
    c2_profiles = ["http"]
    support_browser_scripts = [
        BrowserScript(script_name="create_table", author="@its_a_feature_")
    ]

    async def build(self) -> BuildResponse:
        # this function gets called to create an instance of your payload
        resp = BuildResponse(status=BuildStatus.Error)
        # create the payload
        stdout_err = ""
        try:
            agent_build_path = tempfile.TemporaryDirectory(suffix=self.uuid)
            # shutil to copy payload files over
            copy_tree(self.agent_code_path, agent_build_path.name)
            file1 = open("{}/Config.cs".format(agent_build_path.name), "r").read()
            file1 = file1.replace("%UUID%", self.uuid)
            file1 = file1.replace("%CHUNK_SIZE%", self.get_parameter("chunk_size"))
            file1 = file1.replace(
                "%DEFAULT_PROXY%", self.get_parameter("default_proxy")
            )
            profile = None
            for c2 in self.c2info:
                profile = c2.get_c2profile()["name"]
                for key, val in c2.get_parameters_dict().items():
                    if isinstance(val, dict):
                        file1 = file1.replace(key, val["enc_key"] if val["enc_key"] is not None else "")
                    elif key == "headers":
                        hl = val
                        hl = {n["key"]:n["value"] for n in hl}
                        file1 = file1.replace("USER_AGENT", hl["User-Agent"])
                        if "Host" in hl:
                            file1 = file1.replace("domain_front", hl["Host"])
                        else:
                            file1 = file1.replace("domain_front", "")
                    else:
                        file1 = file1.replace(key, val)
            with open("{}/Config.cs".format(agent_build_path.name), "w") as f:
                f.write(file1)
            defines = ["TRACE"]
            if profile == "http":
                if (
                    self.c2info[0].get_parameters_dict()["encrypted_exchange_check"]
                    == "T"
                ):
                    defines.append("DEFAULT_EKE")
                elif self.c2info[0].get_parameters_dict()["AESPSK"]["enc_key"] is not None:
                    defines.append("DEFAULT_PSK")
                else:
                    defines.append("DEFAULT")
            if self.get_parameter("version") == "4.0":
                defines.append("NET_4")
            if self.get_parameter("cert_pinning") is False:
                defines.append("CERT_FALSE")
            command = 'nuget restore ; msbuild -p:TargetFrameworkVersion=v{} -p:OutputType="{}" -p:Configuration="{}" -p:Platform="{}" -p:DefineConstants="{}"'.format(
                "3.5" if self.get_parameter("version") == "3.5" else "4.0",
                "WinExe",
                "Release",
                "x86" if self.get_parameter("arch") == "x86" else "x64",
                " ".join(defines),
            )
            proc = await asyncio.create_subprocess_shell(
                command,
                stdout=asyncio.subprocess.PIPE,
                stderr=asyncio.subprocess.PIPE,
                cwd=agent_build_path.name,
            )
            stdout, stderr = await proc.communicate()
            stdout_err = ""
            if stdout:
                stdout_err += f"[stdout]\n{stdout.decode()}\n"
            if stderr:
                stdout_err += f"[stderr]\n{stderr.decode()}"
            if os.path.exists("{}/Atlas.exe".format(agent_build_path.name)):
                # we successfully built an exe, see if we need to convert it to shellcode
                if self.get_parameter("output_type") != "Raw":
                    resp.payload = open(
                        "{}/Atlas.exe".format(agent_build_path.name), "rb"
                    ).read()
                    resp.set_message("Successfully Built")
                    resp.status = BuildStatus.Success
                else:
                    command = "chmod 777 {}/donut; chmod +x {}/donut".format(
                        agent_build_path.name, agent_build_path.name
                    )
                    proc = await asyncio.create_subprocess_shell(
                        command,
                        stdout=asyncio.subprocess.PIPE,
                        stderr=asyncio.subprocess.PIPE,
                        cwd=agent_build_path.name,
                    )
                    stdout, stderr = await proc.communicate()
                    stdout_err += "Changing donut to be executable..."
                    stdout_err += stdout.decode()
                    stdout_err += stderr.decode()
                    stdout_err += "Done."
                    if (
                        self.get_parameter("arch") == "x64"
                        or self.get_parameter("arch") == "Any CPU"
                    ):
                        command = "{}/donut -f 1 -a 2 {}/Atlas.exe".format(
                            agent_build_path.name, agent_build_path.name
                        )
                    else:
                        command = "{}/donut -f 1 -a 1 {}/Atlas.exe".format(
                            agent_build_path.name, agent_build_path.name
                        )
                    proc = await asyncio.create_subprocess_shell(
                        command,
                        stdout=asyncio.subprocess.PIPE,
                        stderr=asyncio.subprocess.PIPE,
                        cwd=agent_build_path.name,
                    )
                    stdout, stderr = await proc.communicate()
                    if stdout:
                        stdout_err += f"[stdout]\n{stdout.decode()}"
                    if stderr:
                        stdout_err += f"[stderr]\n{stderr.decode()}"
                    if os.path.exists("{}/loader.bin".format(agent_build_path.name)):
                        resp.payload = open(
                            "{}/loader.bin".format(agent_build_path.name), "rb"
                        ).read()
                        resp.status = BuildStatus.Success
                        resp.message = (
                            "Successfully used Donut to generate Raw Shellcode"
                        )
                    else:
                        resp.status = BuildStatus.Error
                        resp.message = stdout_err
                        resp.payload = b""
            else:
                # something went wrong, return our errors
                resp.set_message(stdout_err)
        except Exception as e:
            resp.set_status(BuildStatus.Error)
            resp.message = "Error building payload: " + str(e) + "\n" + stdout_err
        return resp
