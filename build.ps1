get-childitem -recurse bin -Directory | remove-item -recurse
get-childitem -recurse obj -Directory | remove-item -recurse

$packages = join-path (get-location) "packages"
if (test-path $packages) { remove-item packages -recurse }

$version = get-content VERSION
$branch = git rev-parse --abbrev-ref HEAD
$hash = git log --pretty=format:%h -1
$suffix = "$branch.$hash"

dotnet publish -c Release -v m --version-suffix $suffix /p:versionprefix=$version /p:packageversion=$version
dotnet pack -o $packages -c release --no-build --version-suffix $suffix /p:versionprefix=$version /p:packageversion=$version
