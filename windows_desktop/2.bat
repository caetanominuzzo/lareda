copy la-red.exe a\ 
copy la-red.pdb a\ 
copy library.* a\ 

del packet.* /q
del link.*  /q
del hash.*  /q
cd packets
del *.* /q

cd ..

cd a\

del packet.*  /q
del link.*  /q
del hash.* /q 

cd packets
del *.* /q

cd ..


start la-red