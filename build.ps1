param ( [string] $Version, [string] $Configuration = "Release" )

get-childitem -recurse bin -Directory | remove-item -recurse
get-childitem -recurse obj -Directory | remove-item -recurse

$packages = join-path (get-location) "packages"
if (test-path $packages) { remove-item packages -recurse }

if ([String]::IsNullOrWhiteSpace($version)) { $version = get-content VERSION }

$branch = git rev-parse --abbrev-ref HEAD
$hash = git log --pretty=format:%h -1
$suffix = "$branch.$hash"

dotnet publish -c $Configuration -v n --version-suffix $suffix /p:versionprefix=$version /p:packageversion=$version
dotnet pack    -c $Configuration -v n --version-suffix $suffix /p:versionprefix=$version /p:packageversion=$version -o $packages --no-build 
