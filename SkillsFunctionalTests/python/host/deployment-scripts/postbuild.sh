# Helpers
# -------

exitWithMessageOnError () {
  if [ ! $? -eq 0 ]; then
    echo "An error has occurred during web site deployment."
    echo $1
    exit 1
  fi
}

# Deployment
# ----------

# 1. Install python packages

if [[ ! -n "$SDK_VERSION" ]]; then
  echo installing latest preview version
  pip install --no-deps --no-cache-dir --pre --force-reinstall -i https://test.pypi.org/simple/ botbuilder-core
  pip install --no-deps --no-cache-dir --pre --force-reinstall -i https://test.pypi.org/simple/ botbuilder-schema
  pip install --no-deps --no-cache-dir --pre --force-reinstall -i https://test.pypi.org/simple/ botframework-connector
elif [ "$SDK_VERSION" = "stable" ]; then
  echo installing latest stable version
  pip install --no-deps --no-cache-dir --force-reinstall botbuilder-core
  pip install --no-deps --no-cache-dir --force-reinstall botbuilder-schema
  pip install --no-deps --no-cache-dir --force-reinstall botframework-connector
else
  echo installing version $SDK_VERSION
    pip install --no-deps --no-cache-dir --pre --force-reinstall -i https://test.pypi.org/simple/ botbuilder-core==$SDK_VERSION
    pip install --no-deps --no-cache-dir --pre --force-reinstall -i https://test.pypi.org/simple/ botbuilder-schema==$SDK_VERSION
    pip install --no-deps --no-cache-dir --pre --force-reinstall -i https://test.pypi.org/simple/ botframework-connector==$SDK_VERSION
fi

exitWithMessageOnError "Error installing the BotBuilder packages"