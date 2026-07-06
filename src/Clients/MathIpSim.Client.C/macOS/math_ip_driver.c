#include "math_ip_driver.h"
#include <string.h>
#include <fcntl.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <unistd.h>
#include <stdio.h>

#define SHM_SIZE 0x40000
#define DATA_BASE 0x20000
#define REG_BASE 0x39000

static int g_fd = -1;
static void* g_pBuf = MAP_FAILED;

bool math_ip_init(const char* shm_name) {
    math_ip_cleanup();

    // Map using the physical backing file in /tmp/ to match the C# macOS wrapper
    char filepath[256];
    snprintf(filepath, sizeof(filepath), "/tmp/%s", shm_name);

    g_fd = open(filepath, O_RDWR);
    if (g_fd < 0) {
        return false;
    }

    g_pBuf = mmap(
        NULL,
        SHM_SIZE,
        PROT_READ | PROT_WRITE,
        MAP_SHARED,
        g_fd,
        0
    );

    if (g_pBuf == MAP_FAILED) {
        close(g_fd);
        g_fd = -1;
        return false;
    }

    return true;
}

void math_ip_cleanup(void) {
    if (g_pBuf != MAP_FAILED && g_pBuf != NULL) {
        munmap(g_pBuf, SHM_SIZE);
        g_pBuf = MAP_FAILED;
    }
    if (g_fd >= 0) {
        close(g_fd);
        g_fd = -1;
    }
}

volatile MathIpRegisters* math_ip_get_regs(void) {
    if (g_pBuf == NULL || g_pBuf == MAP_FAILED) {
        return NULL;
    }
    return (volatile MathIpRegisters*)((uint8_t*)g_pBuf + REG_BASE);
}

void* math_ip_get_data_ptr(void) {
    if (g_pBuf == NULL || g_pBuf == MAP_FAILED) {
        return NULL;
    }
    return (void*)((uint8_t*)g_pBuf + DATA_BASE);
}

bool math_ip_write_data(uint32_t offset, const int16_t* data, uint32_t len) {
    void* data_ptr = math_ip_get_data_ptr();
    if (data_ptr == NULL || data == NULL) {
        return false;
    }

    // Boundary check
    uint32_t bytes_to_write = len * sizeof(int16_t);
    if (offset + bytes_to_write > 0x10000) {
        return false; // Out of bounds
    }

    memcpy((uint8_t*)data_ptr + offset, data, bytes_to_write);
    return true;
}

bool math_ip_read_data(uint32_t offset, int16_t* dest, uint32_t len) {
    void* data_ptr = math_ip_get_data_ptr();
    if (data_ptr == NULL || dest == NULL) {
        return false;
    }

    // Boundary check
    uint32_t bytes_to_read = len * sizeof(int16_t);
    if (offset + bytes_to_read > 0x10000) {
        return false; // Out of bounds
    }

    memcpy(dest, (uint8_t*)data_ptr + offset, bytes_to_read);
    return true;
}
