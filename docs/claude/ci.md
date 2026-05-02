# CI/CD

GitHub Actions (`release.yml`) triggers on push to `main`:
1. Reads version from `Unity-Package/Assets/root/package.json`
2. Skips if the version tag already exists on GitHub
3. Builds the `.unitypackage` installer using Unity 2022.3.62f3
4. Runs EditMode, PlayMode, and Standalone tests for each supported Unity version
5. Creates a GitHub release and publishes to OpenUPM

Test projects in `Unity-Tests/` pull the package from the local `Unity-Package/` directory and are used exclusively by CI.
