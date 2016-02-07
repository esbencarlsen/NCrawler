ALTER DATABASE [$(DatabaseName)]
    ADD LOG FILE (NAME = [NCrawler_log], FILENAME = '$(Path1)NCrawler_log.ldf', MAXSIZE = 2097152 MB, FILEGROWTH = 10 %);

