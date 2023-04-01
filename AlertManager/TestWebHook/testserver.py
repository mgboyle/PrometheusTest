from flask import Flask, request

app = Flask(__name__)

@app.route('/alert', methods=['POST'])
def alert():
    print(request.json)
    return 'OK'

if __name__ == '__main__':
    app.run()
