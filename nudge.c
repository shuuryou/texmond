#define _GNU_SOURCE

#include <signal.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/types.h>
#include <unistd.h>

int main(int argc, char *argv[]);

int main(int argc, char *argv[])
{
    char *tempfname, *destfname, *content, *pidfile;
    int pid;

    if (argc != 4)
    {
        fprintf(stderr, "Usage: %s <FILE> <CONTENT> <PIDFILE>\n", argv[0]);
        return 1;
    }

    destfname = argv[1];
    content = argv[2];
    pidfile = argv[3];

    if (access(destfname, F_OK) == 0)
    {
        fprintf(stderr, "Refusing to overwrite existing file \"%s\"!\n", destfname);
        return 2;
    }

    if (access(pidfile, R_OK) != 0)
    {
        fprintf(stderr, "PID file \"%s\" is not readable!\n", pidfile);
        return 3;
    }

    {
        uid_t uid = getuid(), euid = geteuid();
        if (uid > 0 && uid == euid)
        {
            fprintf(stderr, "You must run this program as root or make it suid root.\n");
            return 4;
        }
    }

    pid = -1;

    {
        FILE* pidfp;

        if (!(pidfp = fopen(pidfile, "r")))
        {
            perror("Failed to open PID file: ");
            return 5;
        }

        fscanf(pidfp, "%d", &pid);
        fclose(pidfp);
    }

    if (!asprintf(&tempfname, "%s_tmpXXXXXX", destfname))
    {
        perror("Cannot format temporary file name: ");
        return 6;
    }

    {
        int fd = mkstemp(tempfname);

        if (fd == -1)
        {
            free(tempfname);
            perror("Failed to open temporary file: ");
            return 7;
        }

        dprintf(fd, content);
    }

    if (rename(tempfname, destfname) == -1)
    {
        free(tempfname);
        perror("Failed to rename temporary file to destination file: ");
        return 8;
    }

    free(tempfname);

    kill(pid, SIGUSR1);

    return 0;
}
