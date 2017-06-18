declare -a dirs=("DatedQuickSaves" "EditorTime" "NIMBY" "SensibleScreenshot" "ShipSaveSplicer" "TreeToppler" "Wwwwwwwww")
declare -a builds=("Dated_QuickSaves" "EditorTime" "NotInMyBackYard" "Sensible_Screenshot" "ShipSaveSplicer" "TreeToppler" "Wwwwwwwww")
declare -a magiCore=(1 0 0 1 0 0 0)
arrLen=${#dirs[@]}

mkdir -p build/GameData
for (( i=0; i<${arrLen}; i++ )); do
  d="${dirs[$i]}"
  b="${builds[$i]}"
  m=${magiCore[$i]}
  cp -r GameData/"${d}" build/GameData
  cp "${b}"/bin/Release/*.dll build/GameData/"${d}"/
  rm -f build/GameData/"${d}"/MagiCore.dll

  if [ ${m} -eq 1 ]; then
	cp -r ../MagiCore/GameData/* build/GameData/
  fi

  cd build
  zip -r "${d}".zip GameData
  mv "${d}".zip ../
  cd ..
  rm -r build/GameData/*
done

rm -r build