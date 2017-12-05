# GenerateAndSortBigFile

## The example includes two apps.

The first app generates text file of predefined size. File consists of next format lines: number and string divided dy point. 

Example:

415. Apple
30432. Something something something
1. Apple
32. Cherry is the best
2. Banana is yellow

Second app sorts generated file by next criteria: string value is compared first and if they are equal then number part is compared. The file can be very big size ~100Gb.
  
Example:

1. Apple
415. Apple
2. Banana is yellow
32. Cherry is the best
30432. Something something something