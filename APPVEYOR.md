# A convention based AppVeyor configuration

1. Ensure the target repository matches the conventions outlined below.
2. Copy the files in this repository to the root of the target repository.
3. Add the target repository as an AppVeyor project.
4. CI goodness!


## Conventions

1. The solution uses the new `dnx` build system.
2. Unit test projects within the solution end in `.Tests`.
3. Unit tests are run via the project's `test` command.
4. All non-test projects can be packaged by `dnu pack`.
5. The solution is hosted on GitHub.


## Build process

Every commit to the `master` branch follows this process in a fresh AppVeyor
build environment:

1. The solution is cloned from GitHub into the build environment.
2. The latest CLR is installed. (`dnvm install latest`)
3. The dependencies of all projects are installed. (`dnu restore`)
4. All non-test projects are compiled and packaged into NuGet packages. (`dnu
   pack`)
5. All test projects are run. (`dnx <project> test`)
6. If this is a tagged commit (i.e. a release), the NuGet packages are deployed
   to GitHub as downloadable artifacts.
7. The NuGet packages are deployed to the Sharper.C MyGet feed.
