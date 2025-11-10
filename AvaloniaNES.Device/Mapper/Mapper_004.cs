using AvaloniaNES.Device.Cart;

namespace AvaloniaNES.Device.Mapper
{
    public class Mapper_004 : IMapperService
    {
        private byte _prgBank;
        private byte _chrBank;

        private byte targetRegister = 0x00;
        private bool isPrgBankMode = false;
        private bool isChrInversion = false;
        private MirroringType mirroringType = MirroringType.Horizontal;

        private uint[] pRegister = new uint[8];
        private uint[] pChrBank = new uint[8];
        private uint[] pPrgBank = new uint[8];

        private bool bIRQActive = false;
        private bool bIRQEnable = false;
        private bool bIRQUpdate = false;
        private ushort nIRQCounter = 0;
        private ushort nIRQReload = 0;
        private byte[] _ram = new byte[32 * 1024];

        public bool CPUMapRead(ushort address, ref uint mapAddress, ref byte data)
        {
            if (address >= 0x6000 && address <= 0x7FFF)  // Cartridge RAM
            {
                mapAddress = 0xFFFFFFFF; // Indicate to use returned data
                data = _ram[address & 0x1FFF];
                return true;
            }
            if (address >= 0x8000 && address <= 0x9FFF)
            {
                // Implement PRG bank switching logic here based on isPrgBankMode and pPrgBank
                // This is a placeholder implementation
                mapAddress = (uint)(pPrgBank[0] + (address & 0x1FFF));
                return true;
            }
            if (address >= 0xA000 && address <= 0xBFFF)
            {
                // Implement PRG bank switching logic here based on isPrgBankMode and pPrgBank
                // This is a placeholder implementation
                mapAddress = (uint)(pPrgBank[1] + (address & 0x1FFF));
                return true;
            }
            if (address >= 0xC000 && address <= 0xDFFF)
            {
                // Implement PRG bank switching logic here based on isPrgBankMode and pPrgBank
                // This is a placeholder implementation
                mapAddress = (uint)(pPrgBank[2] + (address & 0x1FFF));
                return true;
            }
            if (address >= 0xE000 && address <= 0xFFFF)
            {
                // Implement PRG bank switching logic here based on isPrgBankMode and pPrgBank
                // This is a placeholder implementation
                mapAddress = (uint)(pPrgBank[3] + (address & 0x1FFF));
                return true;
            }
            return false;
        }

        public bool CPUMapWrite(ushort address, ref uint mapAddress, byte data)
        {
            if (address >= 0x6000 && address <= 0x7FFF)  // Cartridge RAM
            {
                mapAddress = 0xFFFFFFFF; // Indicate to use internal RAM
                _ram[address & 0x1FFF] = data;
                return true;
            }
            if (address >= 0x8000 && address <= 0x9FFF)
            {
                if ((address & 0x0001) == 0)
                {
                    targetRegister = (byte)(data & 0x07);
                    isPrgBankMode = (data & 0x40) != 0;
                    isChrInversion = (data & 0x80) != 0;
                }
                else
                {
                    pRegister[targetRegister] = data;
                    // Update banks based on pRegister values
                    // This is a placeholder implementation
                    if (isChrInversion)
                    {
                        pChrBank[0] = pRegister[2] * 0x0400;
                        pChrBank[1] = pRegister[3] * 0x0400;
                        pChrBank[2] = pRegister[4] * 0x0400;
                        pChrBank[3] = pRegister[5] * 0x0400;
                        pChrBank[4] = (pRegister[0] & 0xFE) * 0x0400;
                        pChrBank[5] = pRegister[0] * 0x0400 + 0x0400;
                        pChrBank[6] = (pRegister[1] & 0xFE) * 0x0400;
                        pChrBank[7] = pRegister[1] * 0x0400 + 0x0400;
                    }
                    else
                    {
                        pChrBank[0] = (pRegister[0] & 0xFE) * 0x0400;
                        pChrBank[1] = pRegister[0] * 0x0400 + 0x0400;
                        pChrBank[2] = (pRegister[1] & 0xFE) * 0x0400;
                        pChrBank[3] = pRegister[1] * 0x0400 + 0x0400;
                        pChrBank[4] = pRegister[2] * 0x0400;
                        pChrBank[5] = pRegister[3] * 0x0400;
                        pChrBank[6] = pRegister[4] * 0x0400;
                        pChrBank[7] = pRegister[5] * 0x0400;
                    }

                    if (isPrgBankMode)
                    {
                        pPrgBank[2] = (pRegister[6] & 0x3F) * 0x2000;
                        pPrgBank[0] = (uint)((_prgBank * 2 - 2) * 0x2000);
                    }
                    else
                    {
                        pPrgBank[0] = (pRegister[6] & 0x3F) * 0x2000;
                        pPrgBank[2] = (uint)((_prgBank * 2 - 2) * 0x2000);
                    }

                    pPrgBank[1] = (pRegister[7] & 0x3F) * 0x2000;
                    pPrgBank[3] = (uint)((_prgBank * 2 - 1) * 0x2000);
                }
                return false;
            }

            if (address >= 0xA000 && address <= 0xBFFF)
            {
                if ((address & 0x0001) == 0)
                {
                    if ((data & 0x01) > 0)
                    {
                        mirroringType = MirroringType.Horizontal;
                    }
                    else
                    {
                        mirroringType = MirroringType.Vertical;
                    }
                }
                else
                {
                }
                return false;
            }

            if (address >= 0xC000 && address <= 0xDFFF)
            {
                if ((address & 0x0001) == 0)
                {
                    nIRQReload = data;
                }
                else
                {
                    nIRQCounter = 0;
                }
                return false;
            }

            if (address >= 0xE000 && address <= 0xEFFF)
            {
                if ((address & 0x0001) == 0)
                {
                    bIRQEnable = false;
                    bIRQActive = false;
                }
                else
                {
                    bIRQEnable = true;
                }
                return false;
            }

            return false;
        }

        public MirroringType GetMirrorType()
        {
            return mirroringType;
        }

        public void MapperInit(byte prgBanks, byte chrBanks)
        {
            _prgBank = prgBanks;
            _chrBank = chrBanks;
            Reset();
        }

        public bool PPUMapRead(ushort address, ref uint mapAddress)
        {
            if (address <= 0x03FF)
            {
                mapAddress = pChrBank[0] + (uint)(address & 0x03FF);
                return true;
            }
            if (address >= 0x0400 && address <= 0x07FF)
            {
                mapAddress = pChrBank[1] + (uint)(address & 0x03FF);
                return true;
            }
            if (address >= 0x0800 && address <= 0x0BFF)
            {
                mapAddress = pChrBank[2] + (uint)(address & 0x03FF);
                return true;
            }
            if (address >= 0x0C00 && address <= 0x0FFF)
            {
                mapAddress = pChrBank[3] + (uint)(address & 0x03FF);
                return true;
            }
            if (address >= 0x1000 && address <= 0x13FF)
            {
                mapAddress = pChrBank[4] + (uint)(address & 0x03FF);
                return true;
            }
            if (address >= 0x1400 && address <= 0x17FF)
            {
                mapAddress = pChrBank[5] + (uint)(address & 0x03FF);
                return true;
            }
            if (address >= 0x1800 && address <= 0x1BFF)
            {
                mapAddress = pChrBank[6] + (uint)(address & 0x03FF);
                return true;
            }
            if (address >= 0x1C00 && address <= 0x1FFF)
            {
                mapAddress = pChrBank[7] + (uint)(address & 0x03FF);
                return true;
            }

            return false;
        }

        public bool PPUMapWrite(ushort address, ref uint mapAddress)
        {
            return false;
        }

        public void Reset()
        {
            targetRegister = 0x00;
            isPrgBankMode = false;
            isChrInversion = false;
            mirroringType = MirroringType.Horizontal;

            bIRQActive = false;
            bIRQEnable = false;
            bIRQUpdate = false;
            nIRQCounter = 0x0000;
            nIRQReload = 0x0000;

            for (int i = 0; i < 4; i++) pPrgBank[i] = 0;
            for (int i = 0; i < 8; i++) { pChrBank[i] = 0; pRegister[i] = 0; }

            pPrgBank[0] = 0 * 0x2000;
            pPrgBank[1] = 1 * 0x2000;
            pPrgBank[2] = (uint)((_prgBank * 2 - 2) * 0x2000);
            pPrgBank[3] = (uint)((_prgBank * 2 - 1) * 0x2000);
        }

        public bool irqState()
        {
            return bIRQActive;
        }

        public void irqClear()
        {
            bIRQActive = false;
        }

        public void scanline()
        {
            if (nIRQCounter == 0)
            {
                nIRQCounter = nIRQReload;
            }
            else
            {
                nIRQCounter--;
            }

            if (nIRQCounter == 0 && bIRQEnable)
            {
                bIRQActive = true;
            }
        }
    }
}