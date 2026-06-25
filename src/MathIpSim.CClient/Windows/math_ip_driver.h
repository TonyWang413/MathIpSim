#ifndef MATH_IP_DRIVER_H
#define MATH_IP_DRIVER_H

#include <stdint.h>
#include <stdbool.h>

#pragma pack(push, 4)
typedef struct {
    volatile uint32_t a_address;
    volatile uint32_t b_address;
    volatile uint32_t c_address;
    volatile uint32_t data_len;
    volatile uint32_t go;
    volatile uint32_t status;
} MathIpRegisters;
#pragma pack(pop)

// Initialize shared memory mapping
bool math_ip_init(const char* shm_name);

// Get pointer to the register structure in shared memory (0x39000)
volatile MathIpRegisters* math_ip_get_regs(void);

// Get pointer to the base of the data space in shared memory (0x20000)
void* math_ip_get_data_ptr(void);

// Helper function to write input data to a memory offset
bool math_ip_write_data(uint32_t offset, const int16_t* data, uint32_t len);

// Helper function to read output data from a memory offset
bool math_ip_read_data(uint32_t offset, int16_t* dest, uint32_t len);

// Cleanup mapping
void math_ip_cleanup(void);

#endif // MATH_IP_DRIVER_H
