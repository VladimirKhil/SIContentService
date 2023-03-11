param (
    [string]$tag = "latest"
)

docker run -it -p 5000:5000 vladimirkhil/sicontentservice:$tag