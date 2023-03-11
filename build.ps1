param (
    [string]$tag = "latest"
)

docker build . -f src\SIContentService\Dockerfile -t vladimirkhil/sicontentservice:$tag