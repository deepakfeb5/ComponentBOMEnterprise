from PIL import Image; Image.open("CG.png").save("app.ico", format="ICO", sizes=[(256,256)])

# Load PNG image
img = Image.open("CG.png")

# Convert to ICO (multiple sizes for better compatibility)
img.save(
    "app.ico",
    format="ICO",
    sizes=[(16,16), (32,32), (48,48), (64,64), (128,128), (256,256)]
)

print("Conversion done: app.ico created")