#include "math_ip_driver.h"
#include <stdio.h>
#include <assert.h>
#include <unistd.h>

int main() {
    printf("[C TEST] Connecting to Shared Memory 'MathIpSharedMemory'...\n");

    // 1. Initialize mapping
    if (!math_ip_init("MathIpSharedMemory")) {
        printf("[C TEST] [ERROR] Failed to map shared memory! Is the C# Daemon running?\n");
        return -1;
    }

    printf("[C TEST] Connected successfully.\n");

    // 2. Prepare inputs
    int16_t a_data[] = { 1, 2, 3 };
    int16_t b_data[] = { 4, 5, 6 };

    // Write to Data Space offsets
    assert(math_ip_write_data(0x1000, a_data, 3));
    assert(math_ip_write_data(0x2000, b_data, 3));

    // 3. Configure Registers
    volatile MathIpRegisters* regs = math_ip_get_regs();
    assert(regs != NULL);

    regs->a_address = 0x1000;
    regs->b_address = 0x2000;
    regs->c_address = 0x3000;
    regs->data_len = 3;

    // 4. Trigger GO
    printf("[C TEST] Triggering calculation (setting GO = 1)...\n");
    regs->go = 1;

    // 5. Polling GO for completion (with timeout)
    int max_retries = 200; // 2 seconds max
    int retries = 0;
    while (regs->go == 1 && retries < max_retries) {
        usleep(10000); // Sleep 10ms
        retries++;
    }

    if (regs->go == 1) {
        printf("[C TEST] [ERROR] Timeout waiting for IP Engine to clear GO!\n");
        math_ip_cleanup();
        return -2;
    }

    printf("[C TEST] Calculation completed in %d ms.\n", retries * 10);
    printf("[C TEST] STATUS: 0x%08X\n", regs->status);
    assert(regs->status == 0); // Should be no error flags

    // 6. Read and assert output results
    int16_t c_data[12];
    assert(math_ip_read_data(0x3000, c_data, 12));

    printf("[C TEST] Verification of output results:\n");
    int16_t expected[] = {
        5, -3, 4, 0,
        7, -3, 10, 0,
        9, -3, 18, 0
    };

    for (int i = 0; i < 12; i++) {
        printf("  Result[%d]: Expected = %d, Got = %d\n", i, expected[i], c_data[i]);
        assert(c_data[i] == expected[i]);
    }

    printf("[C TEST] All assertions PASSED successfully!\n");

    // 7. Cleanup
    math_ip_cleanup();
    return 0;
}
