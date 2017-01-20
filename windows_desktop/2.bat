copy la-red.exe a\ 
copy la-red.pdb a\ 
copy library.* a\ 

del packet.*
del link.* 
del hash.* 
cd packets
del *.*

cd ..

cd a\

del packet.* 
del link.* 
del hash.* 

cd packets
del *.*

cd ..


start la-red