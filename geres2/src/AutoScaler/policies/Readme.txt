Keep this directory since it is supposed to contain AutoScaler policies.
To add a new policy, perform the following steps:
- Add the project with your policy to this solution
- Include an xcopy-statement similar to GeresAutoscalerPolicySamples to copy the resulting policy into this directory
- Include the copied policy binary in the project
- Set the copied policy binaries build-output property to "copy always"