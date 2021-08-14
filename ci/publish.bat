echo off

IF [%1]==[] goto noparam

echo "Build image '%1' and 'latest'..."
docker build --progress plain -f ./Dockerfile -t mylabtools/prometheus-agent:%1 -t mylabtools/prometheus-agent:latest ../src

echo "Publish image '%1' ..."
docker push mylabtools/prometheus-agent:%1

echo "Publish image 'latest' ..."
docker push mylabtools/prometheus-agent:latest

goto done

:noparam
echo "Please specify image version"
goto done

:done
echo "Done!"