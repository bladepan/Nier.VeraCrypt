Nier VeraCrypt Tools
------------------------------------------
Tools to read [VeraCrypt](https://www.veracrypt.fr/) volumes.

# Usage
Right it only supports decrypt a VeraCrypt volume file and dump the decrypted data to a file. You can later mount the output file as a disk.

# Build & Run

## Using dotnet run

```shell
cd Nier.VeraCrypt.Tools

# help
dotnet run -- --help

# sample
# dump decrypted volume to outputFile
dotnet run -- --password test1 --inputFile ~/Documents/1m.disk --outputFile ~/Documents/1m-out.disk
# verify volume header with password
dotnet run -- --mode VerifyHeader --password test1 --inputFile ~/Documents/1m.disk --verbose
```


## Build the binary

```shell
dotnet build

cd Nier.VeraCrypt.Tools/bin/Debug/net8.0

# help
./Nier.VeraCrypt.Tools --help

# sample
./Nier.VeraCrypt.Tools --password test1 --inputFile ~/Documents/1m.disk --outputFile ~/Documents/1m-out.disk
```

## Mount the decrypted file
```shell
# create mount point
sudo mkdir /mnt/1m

# mount
sudo mount -o loop  1m-out.disk /mnt/1m

# umount
sudo umount /mnt/1m
```
