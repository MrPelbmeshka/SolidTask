import asyncio
import aiohttp
from aiogram import Bot, Dispatcher, types
from aiogram.filters import Command
from aiogram.client.default import DefaultBotProperties
from aiogram.enums import ParseMode

BOT_TOKEN = ""  # вставь сюда токен от @BotFather

bot = Bot(
    token=BOT_TOKEN,
    default=DefaultBotProperties(parse_mode=ParseMode.HTML)
)
dp = Dispatcher()

async def get_weather(city: str = "Ulyanovsk") -> str:
    """
    Получает погоду из wttr.in в текстовом виде.
    """
    url = f"https://wttr.in/{city}?format=3"  
    async with aiohttp.ClientSession() as session:
        async with session.get(url) as response:
            if response.status == 200:
                return await response.text()
            else:
                return "⚠️ Не удалось получить погоду."

@dp.message(Command("start"))
async def start(message: types.Message):
    await message.answer(
        "Привет! ☀️\n"
        "Я покажу тебе погоду в Ульяновске.\n\n"
        "Напиши /weather чтобы узнать сейчас."
    )

@dp.message(Command("weather"))
async def weather(message: types.Message):
    weather_info = await get_weather("Ulyanovsk")
    await message.answer(f"Погода в Ульяновске:\n{weather_info}")

async def main():
    await dp.start_polling(bot)

if __name__ == "__main__":
    asyncio.run(main())
