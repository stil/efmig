const platformTemplateGen = (name: string, rid: string, platform: string) =>
  `
      # Build ${name}
      - name: Build ${name}
        if: matrix.platform == '${platform}'
        run: dotnet publish --configuration Release --self-contained -p:PublishSingleFile=true -r ${rid}
      #- name: ZIP ${name}
      #  if: matrix.platform == '${platform}'
      #  run: 7z a -tzip artifacts/efmig-${name}-Release.zip ./Efmig/bin/Release/net7.0/${rid}/publish/*
      - name: Upload ${name} artifacts
        if: matrix.platform == '${platform}'
        uses: actions/upload-artifact@v3
        with:
          name: efmig-${name}-Release
          path: Efmig/bin/Release/net7.0/${rid}/publish/
          if-no-files-found: error
          `;

const extraSteps = [
  platformTemplateGen("win-x64", "win10-x64", "windows-latest"),
  platformTemplateGen("win-arm64", "win10-arm64", "windows-latest"),
  platformTemplateGen("linux-x64", "linux-x64", "ubuntu-latest"),
  platformTemplateGen("linux-arm64", "linux-arm64", "ubuntu-latest"),
  platformTemplateGen("osx-x64", "osx-x64", "macos-latest"),
  platformTemplateGen("osx-arm64", "osx.11.0-arm64", "macos-latest"),
].join("");

const template = `
# Auto-generated by workflowgen.ts
# Do not edit manually.

name: Build efmig

on:
  push:
    branches: [ "master" ]
  pull_request:
    branches: [ "master" ]

jobs:
  build:
    strategy:
      matrix:
        platform: [ ubuntu-latest, macos-latest, windows-latest ]
    runs-on: \${{ matrix.platform }}
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET Core SDK \${{ matrix.dotnet-version }}
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 7.0.x

      ${extraSteps}
`.trim();

Deno.writeTextFile(".github/workflows/build.yml", template);