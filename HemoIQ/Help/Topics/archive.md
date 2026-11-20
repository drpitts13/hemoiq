# Open archive

**Tools → Open Archive** reads an archive created by Purge Database and can restore specimens that are no longer in the live database.

1. Click **Browse…** and choose the archive file. The path box shows the file you picked.
2. The summary states what the archive contains.
3. The grid lists each specimen with accession, type, created date, recorded evidence, and status. **Recorded** is the final identification when one was confirmed. Otherwise it is the antibodies stored with the specimen, plus DAT and notes. That text is the archive record, not a new identification. Status tells you whether that accession is already in the live database.
4. **Restore** copies missing specimens back into the database, including the saved analysis snapshot. That snapshot is the record of the last analysis, not a new identification. Restore stays disabled until an archive is loaded. Specimens that are already present are skipped and are not overwritten.
5. When the archive also contains panel lots that the live database does not have, those lots are restored with the specimens, including the saved antigen order. An order already stored for a lot that is still in the database is left as it is.

**Close** leaves the live database unchanged.

The completion message reports how many specimens were restored, how many were skipped because they were already present, and how many panels were restored. Every tab reloads after a successful restore. **Help** opens this chapter.

Store archive files with the same care as the live database. They contain specimen records.
