for /l %%x in (1, 1, 3) do (

copy la-red.exe a%%x\ 
copy *.pdb a%%x\ 
copy *.dll a%%x\ 

del packet.* /q
del link.*  /q
del hash.*  /q

cd packets
del *.* /q
cd ..
cd cache
del *.* /q
cd .. 

cd a%%x\

del packet.*  /q
del link.*  /q
del hash.* /q 

cd packets
del *.* /q
cd ..
cd cache
del *.* /q
cd ..

start la-red

cd..

)

pause