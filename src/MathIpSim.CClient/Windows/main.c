#include "math_ip_driver.h"
#include <stdio.h>
#include <windows.h>

int main() {
    printf("==================================================\n");
    printf("  Math IP Software Simulator - C Demo (Windows)  \n");
    printf("==================================================\n");
    printf("[DEMO] Step 1: Connecting to shared memory...\n");

    // 1. Initialize mapping
    if (!math_ip_init("MathIpSharedMemory")) {
        printf("[DEMO] [ERROR] Failed to map shared memory!\n");
        printf("[DEMO] Make sure the C# Daemon background service is running first.\n");
        return -1;
    }
    printf("[DEMO] Connected to shared memory successfully.\n");

    // 2. Write inputs
    printf("[DEMO] Step 2: Preparing input arrays...\n");
    int16_t a_data[] = { 1, 2, 3 };
    int16_t b_data[] = { 4, 5, 6 };
    
    printf("  Input Array A: 1, 2, 3\n");
    printf("  Input Array B: 4, 5, 6\n");

    // Write input data to offset 0x1000 and 0x2000 in Data Space
    math_ip_write_data(0x1000, a_data, 3);
    math_ip_write_data(0x2000, b_data, 3);
    printf("[DEMO] Inputs written to Data Space.\n");

    // 3. Configure Registers
    printf("[DEMO] Step 3: Configuring hardware registers...\n");
    volatile MathIpRegisters* regs = math_ip_get_regs();
    if (regs == NULL) {
        printf("[DEMO] [ERROR] Could not resolve registers pointer!\n");
        math_ip_cleanup();
        return -2;
    }

    regs->a_address = 0x1000; // Point to Input A
    regs->b_address = 0x2000; // Point to Input B
    regs->c_address = 0x3000; // Point to output offset
    regs->data_len = 3;       // 3 elements

    printf("  Registers configured: A_ADDR=0x1000, B_ADDR=0x2000, C_ADDR=0x3000, LEN=3\n");

    // 4. Trigger GO
    printf("[DEMO] Step 4: Triggering IP execution (GO = 1)...\n");
    regs->go = 1;

    // 5. Polling GO
    printf("[DEMO] Step 5: Polling GO register until completion...\n");
    int polling_count = 0;
    while (regs->go == 1) {
        Sleep(5); // sleep 5ms
        polling_count++;
    }
    printf("[DEMO] IP Engine finished calculation (GO is cleared by hardware).\n");
    printf("[DEMO] STATUS register: 0x%08X (Bit 0: DivByZero, Bit 1: Overflow)\n", regs->status);

    // 6. Read results
    printf("[DEMO] Step 6: Reading outputs from C_ADDRESS...\n");
    int16_t c_data[12]; // 3 elements * 4 operations
    math_ip_read_data(0x3000, c_data, 12);

    printf("  Calculation Output:\n");
    for (int i = 0; i < 3; i++) {
        int idx = i * 4;
        printf("  Dataset %d: a=%d, b=%d\n", i + 1, a_data[i], b_data[i]);
        printf("    Addition (a+b)      = %d\n", c_data[idx + 0]);
        printf("    Subtraction (a-b)   = %d\n", c_data[idx + 1]);
        printf("    Multiplication (a*b)= %d\n", c_data[idx + 2]);
        printf("    Division (a/b)      = %d\n", c_data[idx + 3]);
    }

    // 7. Cleanup
    printf("[DEMO] Step 7: Releasing resources...\n");
    math_ip_cleanup();
    printf("[DEMO] Done.\n");

    return 0;
}
