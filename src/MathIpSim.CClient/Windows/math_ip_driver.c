#include "math_ip_driver.h"
#include <string.h>
#include <windows.h>

#define SHM_SIZE 0x40000
#define DATA_BASE 0x20000
#define REG_BASE 0x39000

static HANDLE g_hMapFile = NULL;
static void* g_pBuf = NULL;

bool math_ip_init(const char* shm_name) {
    math_ip_cleanup();

    // On Windows, the client opens the existing named mapping created by the Daemon.
    g_hMapFile = OpenFileMappingA(
        FILE_MAP_ALL_ACCESS,   // read/write access
        FALSE,                 // do not inherit the name
        shm_name               // name of mapping object
    );

    if (g_hMapFile == NULL) {
        return false;
    }

    g_pBuf = MapViewOfFile(
        g_hMapFile,
        FILE_MAP_ALL_ACCESS,
        0,
        0,
        SHM_SIZE
    );

    if (g_pBuf == NULL) {
        CloseHandle(g_hMapFile);
        g_hMapFile = NULL;
        return false;
    }

    return true;
}

void math_ip_cleanup(void) {
    if (g_pBuf != NULL) {
        UnmapViewOfFile(g_pBuf);
        g_pBuf = NULL;
    }
    if (g_hMapFile != NULL) {
        CloseHandle(g_hMapFile);
        g_hMapFile = NULL;
    }
}

volatile MathIpRegisters* math_ip_get_regs(void) {
    if (g_pBuf == NULL) {
        return NULL;
    }
    return (volatile MathIpRegisters*)((uint8_t*)g_pBuf + REG_BASE);
}

void* math_ip_get_data_ptr(void) {
    if (g_pBuf == NULL) {
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
